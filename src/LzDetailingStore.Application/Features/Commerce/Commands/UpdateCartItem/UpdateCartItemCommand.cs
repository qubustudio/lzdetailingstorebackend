using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.UpdateCartItem;

public sealed record UpdateCartItemCommand(
    Guid UserId,
    Guid ItemId,
    int Quantity) : IRequest<Result>;
