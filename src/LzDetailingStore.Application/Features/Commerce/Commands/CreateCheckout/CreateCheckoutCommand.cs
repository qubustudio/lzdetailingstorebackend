using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout;

public sealed record CreateCheckoutCommand(Guid UserId) : IRequest<Result<CheckoutResultDto>>;

public sealed record CheckoutResultDto(Guid OrderId, string PreferenceUrl);
