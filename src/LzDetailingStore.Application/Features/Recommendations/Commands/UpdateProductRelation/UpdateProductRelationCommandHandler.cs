using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.UpdateProductRelation;

public sealed class UpdateProductRelationCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductRelationCommand, Result<ProductRelationAdminDto>>
{
    public async Task<Result<ProductRelationAdminDto>> Handle(
        UpdateProductRelationCommand request, CancellationToken ct)
    {
        var relation = await dbContext.Set<ProductRelation>()
            .Include(r => r.Product)
            .Include(r => r.RelatedProduct)
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct);

        if (relation is null)
            return Result<ProductRelationAdminDto>.NotFound("Recomendación no encontrada.");

        var status = relation.Status;
        if (request.Status is not null && !ProductRelationLabels.TryParseStatus(request.Status, out status))
            return Result<ProductRelationAdminDto>.Failure(
                new Error("INVALID_STATUS", "El estado no es válido."));

        if (request.Reason is not null)
            relation.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        relation.Review(status, request.ReviewerId, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);

        return ProductRelationMapper.ToAdminDto(relation);
    }
}
