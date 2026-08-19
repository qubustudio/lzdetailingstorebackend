using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteVariant;

public sealed class DeleteVariantCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteVariantCommand, Result>
{
    public async Task<Result> Handle(DeleteVariantCommand request, CancellationToken ct)
    {
        var variant = await dbContext.Set<ProductVariant>()
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (variant is null)
            return Result.NotFound("Variante no encontrada.");

        variant.IsActive = false;

        var product = await dbContext.Set<Product>()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == variant.ProductId, ct);

        product?.RecalculatePrecioBase();

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
