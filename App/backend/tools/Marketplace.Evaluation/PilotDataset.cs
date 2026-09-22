using Marketplace.BLL;

namespace Marketplace.Evaluation;

public sealed record SyntheticBusiness(Guid Id, int Zone, int Category, int PrimaryNeed, int SecondaryNeed,
    int AdvertisedNeed, double Latitude, double Longitude, DateTimeOffset PublishedAt,
    bool Published, bool Suspended, double Activity);
public sealed record SyntheticQuery(int Sequence, DateTimeOffset At, int Zone, int Category, int Need,
    string Surface, double Latitude, double Longitude, double RadiusKm)
{
    public string Context => $"{Surface}/{Zone}/{Category}/{(Surface == "search" ? Need : -1)}";
    public string Stratum => $"{Surface}/{Zone}";
}
public sealed record PilotDataset(int Seed, string GeneratorVersion,
    IReadOnlyList<SyntheticBusiness> Businesses, IReadOnlyList<SyntheticQuery> Queries);

public static class PilotGenerator
{
    private static readonly (double Lat, double Lon)[] Centers =
        [(-31.416281, -64.184022), (-31.4252, -64.1866), (-31.4078, -64.2036)];
    private static readonly DateTimeOffset Start = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static PilotDataset Generate(int seed, int queryCount = 5000)
    {
        if (queryCount is < 1 or > 100000) throw new ArgumentException("Query count must be 1..100000.");
        var random = new StableRandom(seed);
        var businesses = Enumerable.Range(1, 200).Select(i =>
        {
            var zone = i <= 120 ? 0 : i <= 175 ? 1 : 2;
            var center = Centers[zone];
            var primary = random.Next(4);
            var secondary = (primary + 1 + random.Next(3)) % 4;
            // Misleading or incomplete metadata is distinct from actual synthetic capabilities.
            var advertised = random.Next(5) == 0 ? random.Next(4) : primary;
            return new SyntheticBusiness(new Guid(i, 0, 0, new byte[8]), zone, random.Next(4), primary,
                secondary, advertised, center.Lat + (random.Unit() - .5) * .06,
                center.Lon + (random.Unit() - .5) * .06, i % 10 == 0 ? Start.AddDays(30) : Start.AddDays(-60),
                i % 37 != 0, i % 53 == 0, random.Unit());
        }).ToArray();
        var queries = Enumerable.Range(0, queryCount).Select(i =>
        {
            var zone = random.Next(3); var center = Centers[zone];
            return new SyntheticQuery(i, Start.AddTicks(TimeSpan.FromDays(60).Ticks * i / queryCount),
                zone, random.Next(4), random.Next(4), i % 2 == 0 ? "magazine" : "search",
                center.Lat + (random.Unit() - .5) * .012,
                center.Lon + (random.Unit() - .5) * .012, 4);
        }).ToArray();
        return new(seed, "cordoba-businesses-v1", businesses, queries);
    }

    public static IReadOnlyList<RankingCandidate> Candidates(PilotDataset dataset, SyntheticQuery query)
        => dataset.Businesses.Where(x => x.Category == query.Category).Select(x => new RankingCandidate(
            x.Id, query.Surface == "magazine" ? 1 : x.AdvertisedNeed == query.Need ? 1 : x.SecondaryNeed == query.Need ? .65 : .25,
            Distance(x, query), x.PublishedAt, x.Published, x.Suspended, x.Activity)).ToArray();

    public static IReadOnlyDictionary<Guid, int> Judge(PilotDataset dataset, SyntheticQuery query)
        => dataset.Businesses.Where(x => x.Category == query.Category).ToDictionary(x => x.Id, x =>
        {
            var distance = Distance(x, query);
            if (query.Surface == "magazine") return distance <= 1.5 ? 3 : distance <= 3.5 ? 2 : 1;
            if (x.PrimaryNeed == query.Need) return distance <= 2 ? 3 : 2;
            if (x.SecondaryNeed == query.Need) return distance <= 2 ? 2 : 1;
            return 0;
        });

    // Spherical distance for synthetic data only; the public directory uses PostGIS geography.
    private static double Distance(SyntheticBusiness business, SyntheticQuery query)
    {
        const double radians = Math.PI / 180;
        var a = Math.Pow(Math.Sin((business.Latitude - query.Latitude) * radians / 2), 2) +
            Math.Cos(business.Latitude * radians) * Math.Cos(query.Latitude * radians) *
            Math.Pow(Math.Sin((business.Longitude - query.Longitude) * radians / 2), 2);
        return 6371.0088 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }

    private sealed class StableRandom(int seed)
    {
        private uint state = unchecked((uint)seed) ^ 0x9E3779B9u;
        public double Unit()
        {
            if (state == 0) state = 0xA341316Cu;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return state / 4294967296.0;
        }
        public int Next(int maximum) => (int)(Unit() * maximum);
    }
}
