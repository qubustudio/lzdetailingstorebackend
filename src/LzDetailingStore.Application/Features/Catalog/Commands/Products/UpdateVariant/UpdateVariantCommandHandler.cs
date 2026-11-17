using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;

public sealed class UpdateVariantCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVariantCommand, Result>
{
    public async Task<Result> Handle(UpdateVariantCommand request, CancellationToken ct)
    {
        var variant = await dbContext.Set<ProductVariant>()
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (variant is null)
            return Result.NotFound("Presentación no encontrada.");

        var skuDuplicate = await dbContext.Set<ProductVariant>()
            .AnyAsync(v => v.Id != request.Id
                && v.Sku.ToLower() == request.Sku.ToLower(), ct);

        if (skuDuplicate)
            return Result.Conflict($"Ya existe otra presentación con el SKU '{request.Sku}'.");

        var unitExists = await dbContext.Set<MeasurementUnit>()
            .AnyAsync(u => u.Clave == request.SizeUnit, ct);

        if (!unitExists)
            return Result.Failure(new Error("VALIDATION_ERROR", "La unidad de medida no existe."));

        variant.Sku = request.Sku;
        variant.Nombre = request.Nombre;
        variant.PrecioVenta = request.PrecioVenta;
        variant.PrecioCosto = request.PrecioCosto;
        variant.SizeValue = request.SizeValue;
        variant.SizeUnit = request.SizeUnit;
        variant.Barcode = request.Barcode;
        variant.IsActive = request.IsActive;
        variant.SortOrder = request.SortOrder;

        // Stock is an availability flag: keep it as is when omitted, otherwise normalize to available / sold out.
        if (request.Stock.HasValue)
            variant.SetAvailability(request.Stock.Value > 0);

        var product = await dbContext.Set<Product>()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == variant.ProductId, ct);

        product?.RecalculatePrecioBase();

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
