using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Marketplace.BLL;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class OnboardingFixture : IAsyncLifetime
{
    private readonly string databaseName = "onboarding_test_" + Guid.NewGuid().ToString("N");
    private string adminConnection = "";
    public string ConnectionString { get; private set; } = "";
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")
            ?? throw new InvalidOperationException("Set TEST_DATABASE_CONNECTION to an isolated local PostgreSQL server.");
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
        await create.ExecuteNonQueryAsync();
        ConnectionString = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName }.ToString();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Testing")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Marketplace"] = ConnectionString,
                ["FrontendOrigin"] = "http://localhost:3000",
                ["Auth:PermitLimit"] = "1000",
                ["CatalogAi:Enabled"] = "false"
            })));
        // Transitional red-phase fixture: no initializer exists in the scaffold yet.
        using var scope = Factory.Services.CreateScope();
        foreach (var initializer in scope.ServiceProvider.GetServices<IDatabaseInitializer>())
            await initializer.MigrateAsync();
    }

    public HttpClient Client(bool trustedOrigin = true)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        if (trustedOrigin)
        {
            client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
            client.DefaultRequestHeaders.Add("X-Requested-With", "marketplace");
        }
        return client;
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (adminConnection.Length == 0) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin) { CommandTimeout = 180 };
        await drop.ExecuteNonQueryAsync();
    }
}
