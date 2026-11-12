using System.Text.Json;
using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.GenerateProductRelations;

/// <summary>
/// Asks the AI chat client for complementary/similar products in batches, validates every suggestion
/// against the catalog and the existing relations, and stores the survivors as Draft relations (Source = Ai).
/// A failing batch is logged and skipped; the run continues with the next batch.
/// </summary>
public sealed class GenerateProductRelationsCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork,
    ILogger<GenerateProductRelationsCommandHandler> logger,
    IChatClient? chatClient = null)
    : IRequestHandler<GenerateProductRelationsCommand, Result<ProductRelationGenerationSummaryDto>>
{
    public const int BatchSize = 10;
    public const int MaxOutputTokens = 16000;

    private static readonly ProductRelationType[] AllTypes =
        [ProductRelationType.Complementary, ProductRelationType.Similar];

    public async Task<Result<ProductRelationGenerationSummaryDto>> Handle(
        GenerateProductRelationsCommand request, CancellationToken ct)
    {
        if (chatClient is null)
            return Result<ProductRelationGenerationSummaryDto>.Failure(
                new Error("AI_NOT_CONFIGURED", "La generación de recomendaciones no está configurada."), 503);

        var modeLabel = request.Mode == ProductRelationGenerationMode.New
            ? ProductRelationLabels.ModeNew
            : ProductRelationLabels.ModeFill;

        var products = await dbContext.Set<Product>()
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, Brand = p.Brand.Nombre, CategorySlug = p.Category.Slug, p.Description })
            .ToListAsync(ct);

        var catalog = products
            .Select((p, i) => new CatalogEntry(i + 1, p.Id, p.Name, p.Brand, p.CategorySlug, p.Description))
            .ToList();
        var catalogByRef = catalog.ToDictionary(c => c.Ref);

        var existing = await dbContext.Set<ProductRelation>()
            .AsNoTracking()
            .Select(r => new { r.ProductId, r.RelatedProductId, r.Type, r.SortOrder })
            .ToListAsync(ct);

        var state = new GenerationState(
            existing.Select(r => (r.ProductId, r.RelatedProductId, r.Type)),
            existing.GroupBy(r => (r.ProductId, r.Type)).ToDictionary(g => g.Key, g => g.Max(r => r.SortOrder) + 1));

        var (sources, visibleIds) = request.Mode == ProductRelationGenerationMode.New
            ? (SelectNewSources(catalog, existing.Select(r => r.ProductId).ToHashSet()), (HashSet<Guid>?)null)
            : await SelectFillSourcesAsync(catalog, ct);

        if (catalog.Count < 2 || sources.Count == 0)
        {
            logger.LogInformation("Product relation generation ({Mode}): nothing to process", request.Mode);
            return new ProductRelationGenerationSummaryDto(modeLabel, 0, 0, 0, 0);
        }

        var systemPrompt = ProductRelationPrompt.BuildSystemPrompt(catalog);
        int succeeded = 0, failed = 0, created = 0, batchNumber = 0;

        foreach (var batch in sources.Chunk(BatchSize))
        {
            batchNumber++;
            var proposals = new List<ProductRelation>();

            try
            {
                var batchIds = batch.Select(s => s.Entry.Id).ToHashSet();
                var allowedTypes = batch.ToDictionary(s => s.Entry.Id, s => s.Types);
                var batchState = state.BeginBatch();

                var forward = await AskAsync(systemPrompt, ProductRelationPrompt.BuildForwardPrompt(batch), ct);
                foreach (var item in forward.Products ?? [])
                {
                    if (!catalogByRef.TryGetValue(item.Ref, out var source) || !batchIds.Contains(source.Id))
                        continue;

                    foreach (var (type, suggestions) in Pairs(item))
                    {
                        if (!allowedTypes[source.Id].Contains(type))
                            continue;

                        foreach (var suggestion in suggestions)
                        {
                            if (!catalogByRef.TryGetValue(suggestion.Ref, out var related))
                                continue;
                            if (visibleIds is not null && !visibleIds.Contains(related.Id))
                                continue;

                            batchState.TryPropose(source.Id, related.Id, type, suggestion.Reason, proposals);
                        }
                    }
                }

                if (request.Mode == ProductRelationGenerationMode.New)
                {
                    var reverse = await AskAsync(
                        systemPrompt, ProductRelationPrompt.BuildReversePrompt(batch.Select(s => s.Entry)), ct);

                    foreach (var item in reverse.Products ?? [])
                    {
                        if (!catalogByRef.TryGetValue(item.Ref, out var newProduct) || !batchIds.Contains(newProduct.Id))
                            continue;

                        foreach (var (type, suggestions) in Pairs(item))
                        {
                            foreach (var suggestion in suggestions)
                            {
                                // Reverse direction: the existing product lists the new one.
                                if (!catalogByRef.TryGetValue(suggestion.Ref, out var existingProduct))
                                    continue;

                                batchState.TryProposeReverse(existingProduct.Id, newProduct.Id, type, suggestion.Reason, proposals);
                            }
                        }
                    }
                }

                if (proposals.Count > 0)
                {
                    await dbContext.Set<ProductRelation>().AddRangeAsync(proposals, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                }

                batchState.Commit();
                succeeded++;
                created += proposals.Count;

                logger.LogInformation(
                    "Product relation generation ({Mode}): batch {Batch} created {Count} draft relations",
                    request.Mode, batchNumber, proposals.Count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                foreach (var proposal in proposals)
                    dbContext.Entry(proposal).State = EntityState.Detached;

                logger.LogError(ex,
                    "Product relation generation ({Mode}): batch {Batch} failed and was skipped",
                    request.Mode, batchNumber);
            }
        }

        logger.LogInformation(
            "Product relation generation ({Mode}) finished: {Sources} sources, {Ok} batches ok, {Failed} failed, {Created} relations",
            request.Mode, sources.Count, succeeded, failed, created);

        return new ProductRelationGenerationSummaryDto(modeLabel, sources.Count, succeeded, failed, created);
    }

    private static List<(CatalogEntry Entry, IReadOnlyCollection<ProductRelationType> Types)> SelectNewSources(
        List<CatalogEntry> catalog, HashSet<Guid> productsWithRelations) =>
        catalog
            .Where(c => !productsWithRelations.Contains(c.Id))
            .Select(c => (c, (IReadOnlyCollection<ProductRelationType>)AllTypes))
            .ToList();

    private async Task<(List<(CatalogEntry Entry, IReadOnlyCollection<ProductRelationType> Types)>, HashSet<Guid>?)>
        SelectFillSourcesAsync(List<CatalogEntry> catalog, CancellationToken ct)
    {
        var visibleCounts = await dbContext.Set<ProductRelation>()
            .AsNoTracking()
            .Where(r => r.Status == ProductRelationStatus.Approved
                        && r.RelatedProduct.IsActive
                        && r.RelatedProduct.Variants.Any(v => v.IsActive && v.Stock > 0))
            .GroupBy(r => new { r.ProductId, r.Type })
            .Select(g => new { g.Key.ProductId, g.Key.Type, Count = g.Count() })
            .ToListAsync(ct);

        var countLookup = visibleCounts.ToDictionary(x => (x.ProductId, x.Type), x => x.Count);

        var visibleIds = (await dbContext.Set<Product>()
                .AsNoTracking()
                .Where(ProductRelationVisibility.IsVisible)
                .Select(p => p.Id)
                .ToListAsync(ct))
            .ToHashSet();

        var sources = catalog
            .Select(c => (Entry: c, Types: (IReadOnlyCollection<ProductRelationType>)AllTypes
                .Where(t => countLookup.GetValueOrDefault((c.Id, t)) < ProductRelationVisibility.MinVisibleApprovedPerType)
                .ToArray()))
            .Where(s => s.Types.Count > 0)
            .ToList();

        return (sources, visibleIds);
    }

    private async Task<SuggestionResponse> AskAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        ];

        var options = new ChatOptions
        {
            MaxOutputTokens = MaxOutputTokens,
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                ProductRelationPrompt.Schema,
                ProductRelationPrompt.SchemaName,
                ProductRelationPrompt.SchemaDescription)
        };

        var response = await chatClient!.GetResponseAsync(messages, options, ct);

        if (response.FinishReason == ChatFinishReason.ContentFilter)
            throw new InvalidOperationException("The model declined the request (refusal).");
        if (response.FinishReason == ChatFinishReason.Length)
            throw new InvalidOperationException("The model response was truncated (max tokens reached).");

        var text = StripCodeFence(response.Text);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("The model returned an empty response.");

        return JsonSerializer.Deserialize<SuggestionResponse>(text, ProductRelationPrompt.JsonOptions)
               ?? throw new JsonException("The model response could not be parsed.");
    }

    private static IEnumerable<(ProductRelationType Type, List<Suggestion> Suggestions)> Pairs(ProductSuggestions item)
    {
        yield return (ProductRelationType.Complementary, item.Complementary ?? []);
        yield return (ProductRelationType.Similar, item.Similar ?? []);
    }

    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var firstNewLine = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && lastFence > firstNewLine
            ? trimmed[(firstNewLine + 1)..lastFence].Trim()
            : trimmed;
    }

    /// <summary>
    /// Tracks the relation pairs already present (any status, including Rejected) and the next sort order
    /// per (product, type). Batch changes are staged and only merged on <see cref="BatchState.Commit"/>.
    /// </summary>
    private sealed class GenerationState(
        IEnumerable<(Guid ProductId, Guid RelatedProductId, ProductRelationType Type)> existingKeys,
        Dictionary<(Guid ProductId, ProductRelationType Type), int> nextSortOrder)
    {
        private readonly HashSet<(Guid, Guid, ProductRelationType)> _keys = existingKeys.ToHashSet();

        public BatchState BeginBatch() => new(this);

        public sealed class BatchState(GenerationState owner)
        {
            private readonly HashSet<(Guid, Guid, ProductRelationType)> _keys = [];
            private readonly Dictionary<(Guid, ProductRelationType), int> _sortOrders = [];
            private readonly Dictionary<(Guid, ProductRelationType), int> _forwardCounts = [];
            private readonly Dictionary<(Guid, ProductRelationType), int> _reverseCounts = [];

            /// <summary>Forward: capped per (source product, type).</summary>
            public void TryPropose(Guid productId, Guid relatedId, ProductRelationType type, string? reason,
                List<ProductRelation> proposals) =>
                TryAdd(productId, relatedId, type, reason, proposals, _forwardCounts, (productId, type));

            /// <summary>Reverse: capped per (new product, type).</summary>
            public void TryProposeReverse(Guid existingProductId, Guid newProductId, ProductRelationType type,
                string? reason, List<ProductRelation> proposals) =>
                TryAdd(existingProductId, newProductId, type, reason, proposals, _reverseCounts, (newProductId, type));

            public void Commit()
            {
                owner._keys.UnionWith(_keys);
                foreach (var (key, value) in _sortOrders)
                    owner.NextSortOrderMap[key] = value;
            }

            private void TryAdd(Guid productId, Guid relatedId, ProductRelationType type, string? reason,
                List<ProductRelation> proposals, Dictionary<(Guid, ProductRelationType), int> counts,
                (Guid, ProductRelationType) capKey)
            {
                if (productId == relatedId)
                    return;

                var key = (productId, relatedId, type);
                if (owner._keys.Contains(key) || _keys.Contains(key))
                    return;

                var count = counts.GetValueOrDefault(capKey);
                if (count >= ProductRelationVisibility.MaxPerTypePerRequest)
                    return;

                var sortKey = (productId, type);
                var sortOrder = _sortOrders.TryGetValue(sortKey, out var staged)
                    ? staged
                    : owner.NextSortOrderMap.GetValueOrDefault(sortKey);

                proposals.Add(new ProductRelation
                {
                    ProductId = productId,
                    RelatedProductId = relatedId,
                    Type = type,
                    Reason = NormalizeReason(reason),
                    Status = ProductRelationStatus.Draft,
                    Source = ProductRelationSource.Ai,
                    SortOrder = sortOrder
                });

                _keys.Add(key);
                _sortOrders[sortKey] = sortOrder + 1;
                counts[capKey] = count + 1;
            }
        }

        private Dictionary<(Guid, ProductRelationType), int> NextSortOrderMap { get; } = nextSortOrder;

        private static string? NormalizeReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return null;

            var trimmed = reason.Trim();
            return ProductRelationPrompt.Truncate(trimmed, ProductRelation.ReasonMaxLength);
        }
    }
}
