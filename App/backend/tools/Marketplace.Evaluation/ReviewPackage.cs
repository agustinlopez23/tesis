using System.Globalization;
using System.Text;
using System.Text.Json;
using Marketplace.BLL;

namespace Marketplace.Evaluation;

public static class ReviewPackage
{
    private const string Queries = "dataset_version,split,query_id,surface,zone,category,need,radius_km,constraints";
    private const string Candidates = "dataset_version,query_id,business_id,offer_facts,distance_km,limitations,evidence_ref";
    private const string Judgments = "dataset_version,query_id,business_id,assessor_id,grade,reason,rubric_version";
    private const string Resolutions = "dataset_version,query_id,business_id,assessor_1_id,grade_1,assessor_2_id,grade_2,final_grade,status,resolver_id,reason,rubric_version";
    private const string Worksheet = "dataset_version,query_id,business_id,assessor_id,rubric_version,surface,zone,category,need,radius_km,constraints,offer_facts,distance_km,limitations,evidence_ref,grade,reason";
    private static readonly string[] Assessors = ["E1", "E2"];
    private static bool Immutable(string name) => name is not ("reviewers/E1.csv" or "reviewers/E2.csv" or "resolutions.csv");

    public static IReadOnlyDictionary<string, byte[]> Build(string split)
    {
        var datasets = ReviewCases.Seeds(split).Select(seed => PilotGenerator.Generate(seed)).ToArray();
        var selected = ReviewCases.Select(datasets, split);
        var configuration = new RankingConfiguration();
        var ranker = new BusinessRanker(configuration);
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        var queries = new List<string[]>(); var candidates = new List<string[]>();
        var worksheets = Assessors.ToDictionary(id => id, _ => new List<(string Order, string[] Row)>());
        foreach (var data in datasets)
            files[$"inputs/{data.Seed}.json"] = JsonSerializer.SerializeToUtf8Bytes(data, PilotExport.Json);
        foreach (var (data, query) in selected)
        {
            var dataset = ReviewCases.DatasetId(data.Seed);
            var sequence = query.Sequence.ToString(CultureInfo.InvariantCulture);
            var radius = query.RadiusKm.ToString("R", CultureInfo.InvariantCulture);
            var zone = ReviewCases.Zones[query.Zone]; var category = ReviewCases.Categories[query.Category];
            var need = ReviewCases.Need(query);
            const string constraints = "Escenario ficticio. Preferencia por cercanía; respetar el radio indicado.";
            queries.Add([dataset, split, sequence, query.Surface, zone, category, need, radius, constraints]);
            var context = new RankingContext(query.Context, query.At, query.RadiusKm, data.Seed, query.Sequence);
            // Reuse eligibility on the complete candidate universe; never call Judge or take a top-k.
            var eligible = ranker.Rank(PilotGenerator.Candidates(data, query), context, RankingVariant.Proximity,
                new Dictionary<Guid, ExposureBalance>()).Select(x => x.Id).ToHashSet();
            foreach (var item in PilotGenerator.Candidates(data, query).Where(x => eligible.Contains(x.Id)).OrderBy(x => x.Id))
            {
                var business = data.Businesses.Single(x => x.Id == item.Id);
                var id = item.Id.ToString("D"); var offer = ReviewCases.Offer(business);
                var distance = item.DistanceKm!.Value.ToString("R", CultureInfo.InvariantCulture);
                const string limitations = "Sin datos de precios ni horarios. No suponer prestaciones fuera de la oferta descrita.";
                var evidence = $"synthetic:{dataset}/business/{id}";
                candidates.Add([dataset, sequence, id, offer, distance, limitations, evidence]);
                foreach (var assessor in Assessors)
                {
                    var order = ReviewCases.Hash($"{HumanReview.Rubric}|{assessor}|{dataset}|{data.Seed.ToString(CultureInfo.InvariantCulture)}|{sequence}|{id}");
                    worksheets[assessor].Add((order, [dataset, sequence, id, assessor, HumanReview.Rubric,
                        query.Surface, zone, category, need, radius, constraints, offer, distance, limitations, evidence, "", ""]));
                }
            }
        }
        files["queries.csv"] = ReviewCases.Csv(Queries, queries);
        files["candidates.csv"] = ReviewCases.Csv(Candidates, candidates);
        files["judgments.csv"] = ReviewCases.Csv(Judgments, []);
        files["resolutions.csv"] = ReviewCases.Csv(Resolutions, []);
        foreach (var assessor in Assessors)
            files[$"reviewers/{assessor}.csv"] = ReviewCases.Csv(Worksheet,
                worksheets[assessor].OrderBy(x => x.Order, StringComparer.Ordinal).Select(x => x.Row));
        files["reviewers/LEEME.txt"] = Encoding.UTF8.GetBytes("Fichas ficticias; no son negocios reales. Cada persona recibe solo su CSV.\n" +
            "Editar solamente grade y reason. Grados: 3 satisface necesidad y restricciones; 2 útil con limitación no esencial; " +
            "1 marginal; 0 no satisface o viola restricción esencial. Vacío: pendiente/falta información, nunca cero automático.\n" +
            "Explicar 0/1 y falta de información. No consultar al otro evaluador antes de entregar. " +
            "No usar IA para asignar grados. No cambiar fichas; avisar al coordinador ante errores.\n");
        files["configuration.json"] = JsonSerializer.SerializeToUtf8Bytes(configuration, PilotExport.Json);
        files["manifest.json"] = JsonSerializer.SerializeToUtf8Bytes(new { schema = "business-review-package-v1", split,
            synthetic = true, generatorVersion = datasets[0].GeneratorVersion, semanticVersion = ReviewCases.Version, rubricVersion = HumanReview.Rubric,
            seeds = datasets.Select(x => x.Seed), queryCount = queries.Count, candidatePairs = candidates.Count,
            requiredAssessments = candidates.Count * 2, inputQueriesPerSeed = 5000,
            files = files.Where(x => Immutable(x.Key)).ToDictionary(x => x.Key, x => ReviewCases.Hash(x.Value)) }, PilotExport.Json);
        return files;
    }

    public static string Write(string split, string output)
    {
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("Package output must be empty; existing annotations will not be overwritten.");
        var files = Build(split);
        foreach (var (name, bytes) in files)
        {
            var path = Path.Combine(output, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = new FileStream(path, FileMode.CreateNew);
            stream.Write(bytes);
        }
        return ReviewCases.Hash(files["manifest.json"]);
    }

    public static HumanReviewResult Collect(string directory, string digest, string output)
    {
        var package = Read(directory, digest);
        return HumanReviewFiles.WriteFiles(package.Files, output, digest.ToLowerInvariant(), package.ReviewerHashes);
    }

    internal sealed record Verified(string Split, IReadOnlyDictionary<string, byte[]> Files,
        IReadOnlyDictionary<string, string> ReviewerHashes, IReadOnlyDictionary<string, byte[]> OriginalFiles);

    internal static Verified Read(string directory, string digest)
    {
        var manifest = File.ReadAllBytes(Path.Combine(directory, "manifest.json"));
        if (!string.Equals(ReviewCases.Hash(manifest), digest, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Manifest does not match the previously pinned SHA-256.");
        using var document = JsonDocument.Parse(manifest);
        if (!document.RootElement.TryGetProperty("split", out var split) || split.ValueKind != JsonValueKind.String)
            throw new ArgumentException("Invalid package manifest.");
        var expected = Build(split.GetString()!);
        foreach (var (name, bytes) in expected.Where(x => Immutable(x.Key)))
            if (!File.ReadAllBytes(Path.Combine(directory, name)).SequenceEqual(bytes))
                throw new ArgumentException($"Package source or immutable facts changed: {name}.");
        var reviews = new List<string[]>();
        var originals = new Dictionary<string, byte[]>(expected);
        var reviewerHashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var assessor in Assessors)
        {
            var name = $"reviewers/{assessor}.csv";
            var baseline = HumanReviewFiles.Parse(expected[name], name, Worksheet).ToDictionary(Key);
            var responses = File.ReadAllBytes(Path.Combine(directory, name));
            originals[name] = responses;
            reviewerHashes[name] = ReviewCases.Hash(responses);
            foreach (var row in HumanReviewFiles.Parse(responses, name, Worksheet))
            {
                if (!baseline.TryGetValue(Key(row), out var original) || original.Any(x => x.Key is not ("grade" or "reason") && row[x.Key] != x.Value))
                    throw new ArgumentException($"Worksheet facts or assessor changed in {name}.");
                reviews.Add([row["dataset_version"], row["query_id"], row["business_id"], row["assessor_id"],
                    row["grade"], row["reason"], row["rubric_version"]]);
            }
        }
        originals["resolutions.csv"] = File.ReadAllBytes(Path.Combine(directory, "resolutions.csv"));
        var inputs = new Dictionary<string, byte[]>(originals) { ["judgments.csv"] = ReviewCases.Csv(Judgments, reviews) };
        return new(split.GetString()!, inputs, reviewerHashes, originals);
    }
    private static (string, string, string) Key(Dictionary<string, string> row) => (row["dataset_version"], row["query_id"], row["business_id"]);
}
