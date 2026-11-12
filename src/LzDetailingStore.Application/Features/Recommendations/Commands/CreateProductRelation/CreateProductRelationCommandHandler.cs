using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.CreateProductRelation;

/// <summary>Creates a manual relation. Manual relations are approved on creation.</summary>
public sealed class CreateProductRelationCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductRelationCommand, Result<ProductRelationAdminDto>>
{
    public async Task<Result<ProductRelationAdminDto>> Handle(
        CreateProductRelationCommand request, CancellationToken ct)
    {
        if (!ProductRelationLabels.TryParseType(request.Tipo, out var type))
            return Result<ProductRelationAdminDto>.Failure(
                new Error("INVALID_TYPE", "El tipo de recomendación no es válido."));

        if (request.ProductId == request.RelatedProductId)
            return Result<ProductRelationAdminDto>.Failure(
                new Error("SELF_RELATION", "Un producto no puede recomendarse a sí mismo."));

        var product = await dbContext.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        if (product is null)
            return Result<ProductRelationAdminDto>.NotFound("Producto no encontrado.");

        var related = await dbContext.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.RelatedProductId, ct);
        if (related is null)
            return Result<ProductRelationAdminDto>.NotFound("Producto relacionado no encontrado.");

        var siblings = await dbContext.Set<ProductRelation>()
            .Where(r => r.ProductId == request.ProductId && r.Type == type)
            .Select(r => new { r.RelatedProductId, r.SortOrder })
            .ToListAsync(ct);

        if (siblings.Any(s => s.RelatedProductId == request.RelatedProductId))
            return Result<ProductRelationAdminDto>.Conflict(
                "Ya existe una recomendación de este tipo entre ambos productos.");

        var now = DateTime.UtcNow;
        var relation = new ProductRelation
        {
            ProductId = product.Id,
            RelatedProductId = related.Id,
            Type = type,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = ProductRelationStatus.Approved,
            Source = ProductRelationSource.Manual,
            SortOrder = siblings.Count == 0 ? 0 : siblings.Max(s => s.SortOrder) + 1,
            ReviewedAt = now,
            ReviewedBy = request.ReviewerId,
            Product = product,
            RelatedProduct = related
        };

        await dbContext.Set<ProductRelation>().AddAsync(relation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ProductRelationMapper.ToAdminDto(relation);
    }
}
