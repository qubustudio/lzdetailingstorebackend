using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSpecs;

public sealed class ManageProductSpecsCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ManageProductSpecsCommand, Result>
{
    public async Task<Result> Handle(ManageProductSpecsCommand request, CancellationToken ct)
    {
        var productExists = await dbContext.Set<Product>()
            .AnyAsync(p => p.Id == request.ProductId, ct);

        if (!productExists)
            return Result.NotFound("Producto no encontrado.");

        // Remove all existing specs for this product
        var existingSpecs = await dbContext.Set<ProductSpec>()
            .Where(s => s.ProductId == request.ProductId)
            .ToListAsync(ct);

        dbContext.Set<ProductSpec>().RemoveRange(existingSpecs);

        // Add new specs
        var newSpecs = request.Specs.Select(s => new ProductSpec
        {
            ProductId = request.ProductId,
            Label = s.Label,
            Value = s.Value,
            Orden = s.Orden
        });

        await dbContext.Set<ProductSpec>().AddRangeAsync(newSpecs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
