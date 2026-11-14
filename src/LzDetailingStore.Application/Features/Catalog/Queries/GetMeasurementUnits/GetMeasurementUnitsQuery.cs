using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetMeasurementUnits;

/// <summary>The fixed catalog of variant size units, ordered for the admin dropdown.</summary>
public sealed record GetMeasurementUnitsQuery : IRequest<Result<List<MeasurementUnitDto>>>;
