using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProductById;

public sealed class GetAdminProductByIdQueryHandler(DbContext dbContext)
    : IRequestHandler<GetAdminProductByIdQuery, Result<AdminProductDetailDto>>
{
    public async Task<Result<AdminProductDetailDto>> Handle(GetAdminProductByIdQuery request, CancellationToken ct)
    {
        var product = await dbContext.Set<Product>()
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
            return Result<AdminProductDetailDto>.NotFound("Producto no encontrado.");

        var showCosts = request.IncluyeCostos;

        return new AdminProductDetailDto(
            showCosts,
            product.Id,
            product.Name,
            product.Slug,
            product.Sku,
            product.Description,
            product.BrandId,
            product.Brand.Nombre,
            product.CategoryId,
            product.Category.Nombre,
            product.ImageUrl,
            product.ImageAlt,
            product.Badge,
            product.IsActive,
            product.PrecioBase,
            product.PrecioOriginal,
            product.CreatedAt,
            product.UpdatedAt,
            product.Variants
                .OrderBy(v => v.SortOrder)
                .ThenBy(v => v.Nombre)
                .Select(v => new AdminProductVariantDto(
                    v.Id, v.Sku, v.Nombre, v.PrecioVenta,
                    showCosts ? v.PrecioCosto : null,
                    v.SizeValue, v.SizeUnit, v.Barcode,
                    v.Stock > 0,
                    v.IsActive, v.SortOrder))
                .ToList());
    }
}
