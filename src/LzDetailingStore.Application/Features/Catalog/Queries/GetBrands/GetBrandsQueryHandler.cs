using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetBrands;

public sealed class GetBrandsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetBrandsQuery, Result<List<BrandDto>>>
{
    public async Task<Result<List<BrandDto>>> Handle(GetBrandsQuery request, CancellationToken ct)
    {
        var query = dbContext.Set<Brand>().Where(b => b.IsActive);

        if (request.Destacada.HasValue)
            query = query.Where(b => b.Destacada == request.Destacada.Value);

        var brands = await query
            .OrderBy(b => b.Nombre)
            .Select(b => new BrandDto(
                b.Id, b.Nombre, b.Slug, b.LogoUrl,
                b.Descripcion, b.Categoria.HasValue ? b.Categoria.Value.ToString() : null,
                b.Origen, b.Especialidad, b.Destacada, b.ImagenSpotlight))
            .ToListAsync(ct);

        return brands;
    }
}
