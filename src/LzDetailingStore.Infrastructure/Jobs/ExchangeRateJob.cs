using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Business;
using LzDetailingStore.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Jobs;

public sealed class ExchangeRateJob(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache,
    ILogger<ExchangeRateJob> logger) : BackgroundService
{
    public const string CacheKey = "exchange_rate_usd_mxn";
    private static readonly TimeSpan Interval = TimeSpan.FromHours(4);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial fetch on startup
        await FetchAndStoreAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FetchAndStoreAsync(stoppingToken);
        }
    }

    private async Task FetchAndStoreAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var banxicoService = scope.ServiceProvider.GetRequiredService<IBanxicoService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var result = await banxicoService.GetCurrentRateAsync(ct);

            if (result.IsFailure)
            {
                logger.LogWarning("ExchangeRateJob: Failed to fetch rate - {Error}", result.Error?.Message);
                return;
            }

            var rate = result.Value;

            // Save to DB
            var entity = new ExchangeRate
            {
                Rate = rate,
                Source = "Banxico",
                FetchedAt = DateTime.UtcNow
            };

            await dbContext.Set<ExchangeRate>().AddAsync(entity, ct);
            await dbContext.SaveChangesAsync(ct);

            // Update cache
            cache.Set(CacheKey, rate, CacheTtl);

            logger.LogInformation("ExchangeRateJob: Saved USD/MXN = {Rate}", rate);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "ExchangeRateJob: Unhandled exception");
        }
    }
}
