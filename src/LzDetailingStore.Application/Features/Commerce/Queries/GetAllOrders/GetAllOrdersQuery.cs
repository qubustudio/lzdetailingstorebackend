using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAllOrders;

public sealed record GetAllOrdersQuery(
    int Page = 1,
    int Limit = 20,
    OrderStatus? Status = null)
    : IRequest<Result<PagedResult<OrderListDto>>>;
