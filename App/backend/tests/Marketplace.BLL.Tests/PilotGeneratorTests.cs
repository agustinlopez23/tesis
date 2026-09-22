using System.Text.Json;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class PilotGeneratorTests
{
    [Fact]
    public void Dataset_is_seeded_chronological_and_contains_density_and_newcomer_scenarios()
    {
        var dataset = PilotGenerator.Generate(17);
        Assert.Equal(200, dataset.Businesses.Count);
        Assert.Equal(5000, dataset.Queries.Count);
        Assert.Equal(JsonSerializer.Serialize(dataset), JsonSerializer.Serialize(PilotGenerator.Generate(17)));
        Assert.NotEqual(JsonSerializer.Serialize(dataset), JsonSerializer.Serialize(PilotGenerator.Generate(29)));
        Assert.True(dataset.Queries.Select(x => x.At).SequenceEqual(dataset.Queries.Select(x => x.At).Order()));
        Assert.Equal(new[] { 120, 55, 25 }, dataset.Businesses.GroupBy(x => x.Zone).Select(x => x.Count()));
        Assert.Equal(20, dataset.Businesses.Count(x => x.PublishedAt > dataset.Queries[0].At));
        Assert.Equal(2500, dataset.Queries.Count(x => x.Surface == "search"));
    }

    [Fact]
    public void Judgments_are_independent_of_advertising_activity_and_publication_metadata()
    {
        var dataset = PilotGenerator.Generate(17, 20);
        var query = dataset.Queries.First(x => x.Surface == "search");
        var changed = dataset with { Businesses = dataset.Businesses.Select(x => x with
            { AdvertisedNeed = (x.AdvertisedNeed + 1) % 4, Activity = 1 - x.Activity, PublishedAt = x.PublishedAt.AddDays(-200) }).ToArray() };
        Assert.Equal(PilotGenerator.Judge(dataset, query).OrderBy(x => x.Key), PilotGenerator.Judge(changed, query).OrderBy(x => x.Key));
        Assert.NotEqual(JsonSerializer.Serialize(PilotGenerator.Candidates(dataset, query)), JsonSerializer.Serialize(PilotGenerator.Candidates(changed, query)));
        Assert.All(PilotGenerator.Candidates(dataset, query), candidate =>
            Assert.Equal(query.Category, dataset.Businesses.Single(x => x.Id == candidate.Id).Category));
        Assert.Throws<ArgumentException>(() => PilotGenerator.Generate(17, 0));
    }
}
