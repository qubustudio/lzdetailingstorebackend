using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAdminOrderById;

/// <summary>Any order by id, for the admin panel (no ownership check, unlike the customer detail).</summary>
/// <param name="IncluyeMontos">
/// Whether the caller holds <see cref="AdminOrderPermissions.Montos"/>. Resolved by the API layer; defaults to
/// false so money fields are only returned when explicitly allowed.
/// </param>
public sealed record GetAdminOrderByIdQuery(Guid OrderId, bool IncluyeMontos = false)
    : IRequest<Result<AdminOrderDetailDto>>;
