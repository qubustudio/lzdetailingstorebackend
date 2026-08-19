using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.RemoveCartItem;

public sealed record RemoveCartItemCommand(Guid UserId, Guid ItemId) : IRequest<Result>;
