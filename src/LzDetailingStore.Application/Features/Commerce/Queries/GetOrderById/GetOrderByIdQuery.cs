using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId, Guid UserId) : IRequest<Result<OrderDto>>;
