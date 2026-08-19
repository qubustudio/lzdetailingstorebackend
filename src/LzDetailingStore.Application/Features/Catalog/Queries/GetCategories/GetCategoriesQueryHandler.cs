using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler(DbContext dbContext)
    : IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
{
    public async Task<Result<List<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await dbContext.Set<Category>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Orden)
            .Select(c => new CategoryDto(c.Id, c.Nombre, c.Slug, c.Descripcion, c.Orden))
            .ToListAsync(ct);

        return categories;
    }
}
