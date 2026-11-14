using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetMeasurementUnits;

public sealed class GetMeasurementUnitsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetMeasurementUnitsQuery, Result<List<MeasurementUnitDto>>>
{
    public async Task<Result<List<MeasurementUnitDto>>> Handle(GetMeasurementUnitsQuery request, CancellationToken ct)
    {
        var units = await dbContext.Set<MeasurementUnit>()
            .AsNoTracking()
            .OrderBy(u => u.Orden)
            .Select(u => new { u.Clave, u.Nombre })
            .ToListAsync(ct);

        // Inactive variants count too: their sizes are real presentations of the catalog.
        var usedSizes = await dbContext.Set<ProductVariant>()
            .AsNoTracking()
            .Select(v => new { v.SizeUnit, v.SizeValue })
            .Distinct()
            .ToListAsync(ct);

        var sizesByUnit = usedSizes
            .GroupBy(s => s.SizeUnit, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<decimal>)g.Select(s => s.SizeValue).Distinct().Order().ToList(),
                StringComparer.Ordinal);

        return units
            .Select(u => new MeasurementUnitDto(
                u.Clave,
                u.Nombre,
                sizesByUnit.TryGetValue(u.Clave, out var sizes) ? sizes : []))
            .ToList();
    }
}
