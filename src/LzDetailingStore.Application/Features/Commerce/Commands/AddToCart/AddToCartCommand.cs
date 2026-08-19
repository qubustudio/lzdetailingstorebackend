using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.AddToCart;

public sealed record AddToCartCommand(
    Guid UserId,
    Guid VariantId,
    int Quantity) : IRequest<Result>;
