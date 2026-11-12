using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Recommendations.Queries.GetProductRelations;

/// <param name="Estado">"borrador", "aprobado" or "rechazado" (optional).</param>
/// <param name="Tipo">"complementario" or "similar" (optional).</param>
public sealed record GetProductRelationsQuery(
    string? Estado = null,
    Guid? ProductId = null,
    string? Tipo = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<ProductRelationAdminDto>>>;
