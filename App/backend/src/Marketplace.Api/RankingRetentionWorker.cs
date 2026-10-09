using Marketplace.BLL;

namespace Marketplace.Api;

public sealed class RankingRetentionWorker(IServiceScopeFactory scopes, IHostEnvironment environment,
    ILogger<RankingRetentionWorker> logger, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Fixtures migrate after host creation and run retention explicitly.
        if (environment.IsEnvironment("Testing")) return;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1), clock);
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IRankingSnapshotStore>().PurgeAsync(stoppingToken);
                    logger.LogInformation("Ranking retention completed");
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    // Avoid logging payloads or request identifiers from provider exceptions.
                    logger.LogError("Ranking retention failed ({ErrorType}); retrying next hour", error.GetType().Name);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
