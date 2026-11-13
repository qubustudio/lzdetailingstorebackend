using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAllOrders;

/// <param name="Desde">First Mexico City calendar day to include (inclusive).</param>
/// <param name="Hasta">Last Mexico City calendar day to include (inclusive).</param>
/// <param name="Search">Case-insensitive match on order id, customer name or customer email.</param>
/// <param name="IncluyeMontos">
/// Whether the caller holds <see cref="AdminOrderPermissions.Montos"/>. Resolved by the API layer; defaults to
/// false so money fields are only returned when explicitly allowed.
/// </param>
public sealed record GetAllOrdersQuery(
    int Page = 1,
    int Limit = 20,
    OrderStatus? Status = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    string? Search = null,
    bool IncluyeMontos = false)
    : IRequest<Result<AdminOrderPageDto>>
{
    public const int SearchMaxLength = 100;
}
