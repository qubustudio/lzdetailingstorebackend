using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(DbContext dbContext)
    : IRequestHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken ct)
    {
        var product = await dbContext.Set<Product>()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive).OrderBy(v => v.SortOrder))
            .Include(p => p.Images.OrderBy(i => i.Orden))
            .Include(p => p.Specs.OrderBy(s => s.Orden))
            .Include(p => p.ApplicationSteps.OrderBy(a => a.StepNumber))
            .FirstOrDefaultAsync(p => p.Slug == request.Slug && p.IsActive, ct);

        if (product is null)
            return Result<ProductDto>.NotFound("Product not found");

        var dto = new ProductDto(
            product.Id, product.Sku, product.Name, product.Slug, product.Description,
            product.PrecioBase, product.PrecioOriginal,
            product.ImageUrl, product.ImageAlt, product.Rating, product.ReviewCount,
            product.Badge,
            new BrandDto(product.Brand.Id, product.Brand.Nombre, product.Brand.Slug,
                product.Brand.LogoUrl, product.Brand.Descripcion,
                product.Brand.Categoria?.ToString(), product.Brand.Origen,
                product.Brand.Especialidad, product.Brand.Destacada, product.Brand.ImagenSpotlight),
            new CategoryDto(product.Category.Id, product.Category.Nombre,
                product.Category.Slug, product.Category.Descripcion, product.Category.Orden),
            product.Variants.Select(v => new ProductVariantDto(
                v.Id, v.Sku, v.Nombre, v.PrecioVenta,
                v.SizeValue, v.SizeUnit, v.Stock, v.IsActive, v.SortOrder)).ToList(),
            product.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.ImageAlt, i.Orden)).ToList(),
            product.Specs.Select(s => new ProductSpecDto(s.Id, s.Label, s.Value, s.Orden)).ToList(),
            product.ApplicationSteps.Select(a => new ProductStepDto(a.Id, a.StepNumber, a.Title, a.Description)).ToList());

        return dto;
    }
}
