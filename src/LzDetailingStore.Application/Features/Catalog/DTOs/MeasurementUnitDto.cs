namespace LzDetailingStore.Application.Features.Catalog.DTOs;

/// <param name="Tamanos">Distinct sizes already used by variants in this unit (active or not), ascending.</param>
public sealed record MeasurementUnitDto(string Clave, string Nombre, IReadOnlyList<decimal> Tamanos);
