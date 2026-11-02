using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class DeploymentReadinessTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private const string Secret = "proxy-secret-for-tests-0123456789abcdef";

    private static IConfiguration Settings(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Production_settings_accept_a_complete_configuration()
    {
        Assert.Empty(DeploymentSettings.Validate(Settings(new()
        {
            ["ConnectionStrings:Marketplace"] = "Host=db.example.test;Database=postgres;Username=app;Password=strong-value",
            ["FrontendOrigin"] = "https://cerca.example.test",
            ["Proxy:SharedSecret"] = Secret
        })));
    }

    [Fact]
    public void Production_settings_report_every_missing_or_unsafe_value()
    {
        var errors = DeploymentSettings.Validate(Settings(new()
        {
            ["ConnectionStrings:Marketplace"] = "Host=127.0.0.1;Password=configure-locally",
            ["FrontendOrigin"] = "https://cerca.example.test/app",
            ["Proxy:SharedSecret"] = "short"
        }));
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, error => error.Contains("ConnectionStrings__Marketplace", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("FrontendOrigin", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("Proxy__SharedSecret", StringComparison.Ordinal));
        Assert.Equal(3, DeploymentSettings.Validate(Settings(new())).Count);
    }

    [Fact]
    public void Production_host_fails_fast_with_a_clear_message_when_settings_are_missing()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Marketplace"] = fixture.ConnectionString,
                ["FrontendOrigin"] = "https://cerca.example.test"
            })));
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Proxy__SharedSecret", error.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Migrate_on_startup_is_opt_in(bool enabled)
    {
        var admin = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")!;
        var name = "migrate_startup_" + Guid.NewGuid().ToString("N");
        await Execute(admin, $"CREATE DATABASE \"{name}\"");
        var connection = new NpgsqlConnectionStringBuilder(admin) { Database = name }.ToString();
        try
        {
            await using (var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
                .UseEnvironment("Testing")
                .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Marketplace"] = connection,
                    ["FrontendOrigin"] = "http://localhost:3000",
                    ["Database:MigrateOnStartup"] = enabled.ToString()
                }))))
            {
                using var client = factory.CreateClient();
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
            }
            await using var database = new NpgsqlConnection(connection);
            await database.OpenAsync();
            await using var query = new NpgsqlCommand("SELECT to_regclass('\"__EFMigrationsHistory\"') IS NOT NULL", database);
            Assert.Equal(enabled, (bool)(await query.ExecuteScalarAsync())!);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await Execute(admin, $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
        }
    }

    [Fact]
    public async Task Data_protection_keys_are_stored_in_the_database_and_survive_a_restart()
    {
        string protectedValue;
        await using (var first = Host())
            protectedValue = first.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("deployment-test").Protect("cursor");

        await using (var database = new NpgsqlConnection(fixture.ConnectionString))
        {
            await database.OpenAsync();
            await using var count = new NpgsqlCommand("SELECT count(*) FROM data_protection_keys", database);
            Assert.True((long)(await count.ExecuteScalarAsync())! >= 1);
        }

        await using var second = Host();
        Assert.Equal("cursor", second.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("deployment-test").Unprotect(protectedValue));

        WebApplicationFactory<Program> Host() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Testing")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Marketplace"] = fixture.ConnectionString,
                ["FrontendOrigin"] = "http://localhost:3000"
            })));
    }

    [Fact]
    public void Client_address_uses_the_forwarded_ip_only_with_the_shared_secret()
    {
        static HttpContext Request(string? secret, string? forwarded)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
            if (secret is not null) context.Request.Headers[ClientAddress.SecretHeader] = secret;
            if (forwarded is not null) context.Request.Headers[ClientAddress.ClientIpHeader] = forwarded;
            return context;
        }

        Assert.Equal("203.0.113.7", ClientAddress.Resolve(Request(Secret, "203.0.113.7"), Secret));
        Assert.Equal("2001:db8::1", ClientAddress.Resolve(Request(Secret, "2001:db8::1"), Secret));
        Assert.Equal("10.0.0.5", ClientAddress.Resolve(Request("wrong-secret", "203.0.113.7"), Secret));
        Assert.Equal("10.0.0.5", ClientAddress.Resolve(Request(null, "203.0.113.7"), Secret));
        Assert.Equal("10.0.0.5", ClientAddress.Resolve(Request(Secret, "not-an-ip"), Secret));
        Assert.Equal("10.0.0.5", ClientAddress.Resolve(Request(Secret, "203.0.113.7"), null));
        Assert.Equal("10.0.0.5", ClientAddress.Resolve(Request("", "203.0.113.7"), ""));
    }

    [Theory]
    [InlineData(true, HttpStatusCode.Unauthorized)]
    [InlineData(false, HttpStatusCode.TooManyRequests)]
    public async Task Login_rate_limit_partitions_by_the_trusted_client_address(bool trusted, HttpStatusCode second)
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:PermitLimit"] = "1",
                ["Proxy:SharedSecret"] = Secret
            })));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var payload = new { email = "missing@example.test", password = "Wrong-password-123" };

        Task<HttpResponseMessage> Login(string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login") { Content = JsonContent.Create(payload) };
            request.Headers.Add("Origin", "http://localhost:3000");
            request.Headers.Add("X-Requested-With", "marketplace");
            request.Headers.Add(ClientAddress.SecretHeader, trusted ? Secret : "forged-secret");
            request.Headers.Add(ClientAddress.ClientIpHeader, ip);
            return client.SendAsync(request);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Login("203.0.113.1")).StatusCode);
        Assert.Equal(second, (await Login("203.0.113.2")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login("203.0.113.1")).StatusCode);
    }

    private static async Task Execute(string connection, string sql)
    {
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand(sql, admin);
        await command.ExecuteNonQueryAsync();
    }
}
