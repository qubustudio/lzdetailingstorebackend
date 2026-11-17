using LzDetailingStore.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Jobs;

/// <summary>
/// Once a day, clears the buyer's IP and User-Agent from orders older than the retention period
/// (<see cref="OrderClientInfoRetention.RetentionYears"/>). Failures are logged and never stop the host.
/// </summary>
public sealed class OrderClientInfoRetentionJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OrderClientInfoRetentionJob> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // First run shortly after startup, once migrations and seeding are done.
            await Task.Delay(InitialDelay, timeProvider, stoppingToken);
            await RunOnceAsync(stoppingToken);

            using var timer = new PeriodicTimer(Interval, timeProvider);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down.
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cleared = await OrderClientInfoRetention.ClearExpiredAsync(dbContext, timeProvider.GetUtcNow(), ct);

            logger.LogInformation(
                "OrderClientInfoRetentionJob: Cleared client IP/User-Agent from {Count} orders older than {Years} years",
                cleared, OrderClientInfoRetention.RetentionYears);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "OrderClientInfoRetentionJob: Unhandled exception");
        }
    }
}
