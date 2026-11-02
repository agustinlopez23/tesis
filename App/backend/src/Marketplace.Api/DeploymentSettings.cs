using Marketplace.BLL;

namespace Marketplace.Api;

// Settings a hosted (Production) deployment cannot run without. Messages name the environment variable to set.
public static class DeploymentSettings
{
    public const int MinimumSecretLength = 32;

    public static IReadOnlyList<string> Validate(IConfiguration configuration)
    {
        var errors = new List<string>();
        var connection = configuration.GetConnectionString("Marketplace");
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("configure-locally", StringComparison.Ordinal))
            errors.Add("ConnectionStrings__Marketplace must be set to the PostgreSQL/PostGIS connection string.");
        var origin = configuration["FrontendOrigin"];
        if (!IsOrigin(origin))
            errors.Add("FrontendOrigin must be the web origin only, for example https://<app>.vercel.app (no path or trailing slash).");
        if ((configuration["Proxy:SharedSecret"]?.Length ?? 0) < MinimumSecretLength)
            errors.Add($"Proxy__SharedSecret must be set to a random value of at least {MinimumSecretLength} characters (same value as API_PROXY_SECRET on the web).");
        return errors;
    }

    private static bool IsOrigin(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
        string.Equals(value, uri.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal);
}

// Runs before the server accepts traffic: fail fast on incomplete Production settings,
// then apply pending migrations when Database:MigrateOnStartup is enabled.
public sealed class DeploymentStartup(
    IConfiguration configuration,
    IHostEnvironment environment,
    IServiceScopeFactory scopes,
    ILogger<DeploymentStartup> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            var errors = DeploymentSettings.Validate(configuration);
            if (errors.Count > 0)
                throw new InvalidOperationException("Invalid Production configuration:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errors.Select(error => " - " + error)));
        }
        if (!configuration.GetValue("Database:MigrateOnStartup", false)) return;
        logger.LogInformation("Applying pending database migrations before startup.");
        using var scope = scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
