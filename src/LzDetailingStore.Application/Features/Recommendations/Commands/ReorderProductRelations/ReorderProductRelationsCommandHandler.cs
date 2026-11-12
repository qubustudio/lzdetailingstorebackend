using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.ReorderProductRelations;

/// <summary>
/// Rewrites sort_order for the relations of a product and type. Listed ids get 0..n-1 in the given order;
/// relations not listed keep their relative order after them.
/// </summary>
public sealed class ReorderProductRelationsCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderProductRelationsCommand, Result>
{
    public async Task<Result> Handle(ReorderProductRelationsCommand request, CancellationToken ct)
    {
        if (!ProductRelationLabels.TryParseType(request.Tipo, out var type))
            return Result.Failure(new Error("INVALID_TYPE", "El tipo de recomendación no es válido."));

        var productExists = await dbContext.Set<Product>().AnyAsync(p => p.Id == request.ProductId, ct);
        if (!productExists)
            return Result.NotFound("Producto no encontrado.");

        var relations = await dbContext.Set<ProductRelation>()
            .Where(r => r.ProductId == request.ProductId && r.Type == type)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(ct);

        var byId = relations.ToDictionary(r => r.Id);
        if (request.Ids.Any(id => !byId.ContainsKey(id)))
            return Result.Failure(new Error("INVALID_RELATION_IDS",
                "Algunas recomendaciones no pertenecen a este producto o tipo."));

        var ordered = request.Ids.Select(id => byId[id])
            .Concat(relations.Where(r => !request.Ids.Contains(r.Id)))
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
            ordered[i].SortOrder = i;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
