using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.ClearCart;

public sealed record ClearCartCommand(Guid UserId) : IRequest<Result>;
