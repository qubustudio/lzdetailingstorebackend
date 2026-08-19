using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : IRequest<Result>;
