using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.MergeCart;

public sealed record MergeCartItem(Guid VariantId, int Quantity);

public sealed record MergeCartCommand(
    Guid UserId,
    IList<MergeCartItem> Items) : IRequest<Result>;
