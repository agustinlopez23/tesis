using Marketplace.BLL;

namespace Marketplace.Api;

public sealed class ActivityRetentionWorker(IServiceScopeFactory scopes, IHostEnvironment environment,
    ILogger<ActivityRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Test fixtures migrate their isolated databases after host creation and invoke cleanup explicitly.
        if (environment.IsEnvironment("Testing")) return;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var removed = await scope.ServiceProvider.GetRequiredService<ActivityService>().PurgeAsync(stoppingToken);
                    logger.LogInformation("Activity retention completed: {Removed} expired events removed", removed);
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    logger.LogError(error, "Activity retention failed; the next scheduled run will retry");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
