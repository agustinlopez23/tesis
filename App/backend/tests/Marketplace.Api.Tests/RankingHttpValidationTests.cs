using System.Net;
using System.Net.Http.Json;
using System.Text;
using Marketplace.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed partial class RankingHttpTests
{
    [Fact]
    public async Task Post_rejects_untrusted_origin_invalid_filters_types_and_oversized_bodies()
    {
        await using var factory = Preview();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Remove("Origin");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Route, Body())).StatusCode);
        client.DefaultRequestHeaders.Add("Origin", "https://untrusted.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Route, Body())).StatusCode);
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        foreach (var body in new[] { Body() with { RequestId = Guid.Empty }, Body() with { ZoneId = "unknown" },
            Body() with { CategoryId = 0 }, Body() with { CategoryId = 32000 }, Body() with { RadiusKm = 5 } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Route, body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Route + "?q=test", Body())).StatusCode);
        foreach (var json in new[] { "null", "{}", "{", "{\"categoryId\":\"1\"}",
            "{\"requestId\":\"" + Guid.NewGuid() + "\",\"zoneId\":\"centro\",\"categoryId\":1,\"radiusKm\":4,\"seed\":3}" })
        {
            var response = await client.PostAsync(Route, new StringContent(json, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AssertNoStore(response);
        }
        var oversized = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(new string(' ', 4097))));
        oversized.Headers.ContentType = new("application/json");
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.PostAsync(Route, oversized)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Route, new StringContent("{}"))).StatusCode);
    }

    [Fact]
    public async Task Page_rejects_invalid_or_mixed_cursors_and_distinguishes_gone()
    {
        await using var factory = Preview();
        using var client = Client(factory);
        var tokens = factory.Services.GetRequiredService<RankingCursorTokens>();
        var live = tokens.Issue(new(Guid.NewGuid(), 20, DateTimeOffset.UtcNow.AddMinutes(30)));
        foreach (var query in new[] { "", "?cursor=bad", "?cursor=" + live + "&zoneId=centro", "?cursor=" + live + "&cursor=" + live })
        {
            var response = await client.GetAsync(Route + "/page" + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AssertNoStore(response);
        }
        var expired = tokens.Issue(new(Guid.NewGuid(), 20, DateTimeOffset.UtcNow.AddSeconds(-1)));
        foreach (var token in new[] { live, expired })
        {
            var response = await client.GetAsync(Route + "/page?cursor=" + token);
            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
            AssertNoStore(response);
        }
    }
}
