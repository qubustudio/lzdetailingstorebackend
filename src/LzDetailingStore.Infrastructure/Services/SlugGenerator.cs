using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Infrastructure.Services;

public sealed partial class SlugGenerator(AppDbContext dbContext) : ISlugGenerator
{
    public async Task<string> GenerateAsync(string input, string entityTable, CancellationToken ct = default)
    {
        var slug = Slugify(input);

        var exists = entityTable switch
        {
            "products" => await dbContext.Set<Domain.Entities.Catalog.Product>()
                .AnyAsync(p => p.Slug == slug, ct),
            "brands" => await dbContext.Set<Domain.Entities.Catalog.Brand>()
                .AnyAsync(b => b.Slug == slug, ct),
            "categories" => await dbContext.Set<Domain.Entities.Catalog.Category>()
                .AnyAsync(c => c.Slug == slug, ct),
            _ => false
        };

        if (!exists) return slug;

        var counter = 2;
        while (true)
        {
            var candidate = $"{slug}-{counter}";
            var candidateExists = entityTable switch
            {
                "products" => await dbContext.Set<Domain.Entities.Catalog.Product>()
                    .AnyAsync(p => p.Slug == candidate, ct),
                "brands" => await dbContext.Set<Domain.Entities.Catalog.Brand>()
                    .AnyAsync(b => b.Slug == candidate, ct),
                "categories" => await dbContext.Set<Domain.Entities.Catalog.Category>()
                    .AnyAsync(c => c.Slug == candidate, ct),
                _ => false
            };

            if (!candidateExists) return candidate;
            counter++;
        }
    }

    private static string Slugify(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC);
        result = result.ToLowerInvariant();
        result = SlugRegex().Replace(result, " ");
        result = result.Trim();
        result = WhitespaceRegex().Replace(result, "-");

        return result;
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
