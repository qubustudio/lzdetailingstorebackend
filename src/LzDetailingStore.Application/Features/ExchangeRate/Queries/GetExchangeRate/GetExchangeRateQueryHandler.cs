using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LzDetailingStore.Application.Features.ExchangeRate.Queries.GetExchangeRate;

public sealed class GetExchangeRateQueryHandler(
    DbContext dbContext,
    IMemoryCache cache)
    : IRequestHandler<GetExchangeRateQuery, Result<ExchangeRateDto>>
{
    private const string CacheKey = "exchange_rate_usd_mxn";

    public async Task<Result<ExchangeRateDto>> Handle(GetExchangeRateQuery request, CancellationToken ct)
    {
        // Try cache first
        if (cache.TryGetValue<decimal>(CacheKey, out var cachedRate))
        {
            return new ExchangeRateDto(cachedRate, DateTime.UtcNow, "Cache");
        }

        // Fallback to DB (latest record)
        var latest = await dbContext.Set<Domain.Entities.Business.ExchangeRate>()
            .OrderByDescending(e => e.FetchedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is null)
            return Result<ExchangeRateDto>.NotFound("Tipo de cambio no disponible.");

        // Warm cache
        cache.Set(CacheKey, latest.Rate, TimeSpan.FromHours(1));

        return new ExchangeRateDto(latest.Rate, latest.FetchedAt, latest.Source);
    }
}
