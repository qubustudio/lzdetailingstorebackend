using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetOrders;

public sealed record GetOrdersQuery(Guid UserId, int Page = 1, int Limit = 20)
    : IRequest<Result<PagedResult<OrderListDto>>>;
