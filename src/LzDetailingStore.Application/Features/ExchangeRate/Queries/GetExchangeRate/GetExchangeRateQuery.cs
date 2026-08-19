using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.ExchangeRate.Queries.GetExchangeRate;

public sealed record GetExchangeRateQuery : IRequest<Result<ExchangeRateDto>>;

public sealed record ExchangeRateDto(decimal Rate, DateTime FetchedAt, string Source);
