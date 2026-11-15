using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout;

/// <param name="UserId">Authenticated buyer.</param>
/// <param name="ClientIp">Raw remote IP read by the API layer (after forwarded headers); optional.</param>
/// <param name="ClientUserAgent">Raw User-Agent header read by the API layer; optional.</param>
public sealed record CreateCheckoutCommand(Guid UserId, string? ClientIp = null, string? ClientUserAgent = null)
    : IRequest<Result<CheckoutResultDto>>;

public sealed record CheckoutResultDto(Guid OrderId, string PreferenceUrl);
