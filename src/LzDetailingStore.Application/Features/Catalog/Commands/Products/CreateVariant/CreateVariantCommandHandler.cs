using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateVariant;

public sealed class CreateVariantCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVariantCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVariantCommand request, CancellationToken ct)
    {
        var product = await dbContext.Set<Product>()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product is null)
            return Result<Guid>.NotFound("Producto no encontrado.");

        var skuExists = await dbContext.Set<ProductVariant>()
            .AnyAsync(v => v.Sku.ToLower() == request.Sku.ToLower(), ct);

        if (skuExists)
            return Result<Guid>.Conflict($"Ya existe una variante con el SKU '{request.Sku}'.");

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Sku = request.Sku,
            Nombre = request.Nombre,
            PrecioVenta = request.PrecioVenta,
            PrecioCosto = request.PrecioCosto,
            SizeValue = request.SizeValue,
            SizeUnit = request.SizeUnit,
            Stock = request.Stock,
            Barcode = request.Barcode,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        await dbContext.Set<ProductVariant>().AddAsync(variant, ct);

        product.Variants.Add(variant);
        product.RecalculatePrecioBase();

        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(variant.Id);
    }
}
