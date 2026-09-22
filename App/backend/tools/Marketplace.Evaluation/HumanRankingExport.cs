using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.BLL;

namespace Marketplace.Evaluation;

public static class HumanRankingExport
{
    public static bool Write(string package, string digest, string output, RankingConfiguration configuration, RankingVariant variant)
    {
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("Comparison output must be empty; evidence will not be overwritten.");
        Validate(configuration, variant);
        var verified = ReviewPackage.Read(package, digest);
        if (verified.Split != "calibration") throw new ArgumentException("Human comparison accepts calibration packages only.");
        var packageSha256 = digest.ToLowerInvariant();
        var review = HumanReviewFiles.WriteFiles(verified.Files, Path.Combine(output, "review"), packageSha256, verified.ReviewerHashes);
        void WriteStatus() => WriteJson(output, "status.json", new { schema = "human-calibration-status-v1",
            status = review.Complete ? "complete-calibration-comparison" : "pending-human-review", acceptanceEvaluated = false,
            packageSha256, reviewerSha256 = verified.ReviewerHashes, configuration, candidate = variant });
        if (!review.Complete) { WriteStatus(); WriteHashes(output); return false; }
        var datasets = ReviewCases.Seeds("calibration").Select(seed =>
            JsonSerializer.Deserialize<PilotDataset>(verified.Files[$"inputs/{seed}.json"], PilotExport.Json)!).ToArray();
        var sample = ReviewCases.Select(datasets, "calibration");
        var runs = new List<HumanRankingRun>();
        foreach (var dataset in datasets)
        {
            var selected = sample.Where(x => x.Item1.Seed == dataset.Seed).Select(x => x.Item2.Sequence).ToHashSet();
            var judgments = review.FinalJudgments.Where(x => x.Pair.DatasetVersion == ReviewCases.DatasetId(dataset.Seed))
                .GroupBy(x => int.Parse(x.Pair.QueryId, CultureInfo.InvariantCulture))
                .ToDictionary(x => x.Key, x => (IReadOnlyDictionary<Guid, int>)x.ToDictionary(y => Guid.Parse(y.Pair.BusinessId), y => y.Grade));
            using var file = new FileStream(Path.Combine(output, $"traces-{dataset.Seed}.jsonl.gz"), FileMode.CreateNew);
            using var compressed = new GZipStream(file, CompressionLevel.Fastest);
            using var writer = new StreamWriter(compressed, new UTF8Encoding(false)) { NewLine = "\n" };
            foreach (var policy in new[] { RankingVariant.Proximity, variant })
                runs.Add(HumanRankingRunner.Run(dataset, configuration, policy, selected, judgments,
                    trace => writer.WriteLine(JsonSerializer.Serialize(trace, PilotExport.Json))));
        }
        var comparisons = HumanComparison.Summarize(runs, variant);
        WriteJson(output, "summary.json", new { schema = "human-calibration-comparison-v1", scope = "calibration-only",
            synthetic = true, relevanceSource = "verified-human-review", acceptanceEvaluated = false,
            packageSha256, reviewerSha256 = verified.ReviewerHashes, configuration, candidate = variant,
            seeds = datasets.Select(x => x.Seed), runs, comparisons });
        using (var writer = new StreamWriter(new FileStream(Path.Combine(output, "report.md"), FileMode.CreateNew), new UTF8Encoding(false)))
            writer.Write(Report(runs, comparisons, variant));
        WriteStatus(); WriteHashes(output);
        return true;
    }

    private static void Validate(RankingConfiguration configuration, RankingVariant variant)
    {
        if (variant is not (RankingVariant.EquityOnly or RankingVariant.EquityNewcomer or RankingVariant.EquityRotation or RankingVariant.Equity) ||
            configuration with { EquityWeight = .1, NewcomerWeight = .05 } != new RankingConfiguration() ||
            configuration.EquityWeight is not (.025 or .05 or .1) || configuration.NewcomerWeight is not (0 or .025 or .05) ||
            (variant is RankingVariant.EquityOnly or RankingVariant.EquityRotation) != (configuration.NewcomerWeight == 0))
            throw new ArgumentException("Use the calibration grid: Eq=.025/.05/.1; N=0 without novelty or .025/.05 with novelty; other fields frozen.");
        _ = new BusinessRanker(configuration);
    }

    internal static void WriteJson<T>(string output, string name, T value)
    {
        using var file = new FileStream(Path.Combine(output, name), FileMode.CreateNew);
        JsonSerializer.Serialize(file, value, new JsonSerializerOptions(PilotExport.Json) { WriteIndented = true });
    }

    internal static void WriteHashes(string output)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(output, "*", SearchOption.AllDirectories))
        {
            using var input = File.OpenRead(path);
            hashes[Path.GetRelativePath(output, path).Replace('\\', '/')] = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        }
        WriteJson(output, "sha256.json", hashes);
    }

    private static string Report(IReadOnlyList<HumanRankingRun> runs, IReadOnlyList<HumanComparisonRow> rows, RankingVariant candidate)
    {
        static string F(double? value) => value?.ToString("F5", CultureInfo.InvariantCulture) ?? "sin datos";
        var report = new StringBuilder($"# Comparación de calibración: {candidate}\n\n");
        report.Append("Datos ficticios y relevancia de las hojas verificadas. No es una decisión de aceptación ni selecciona una política.\n\n");
        report.Append("Cada semilla pesa igual. Delta = candidata − Proximity; DE muestral de diferencias pareadas, no intervalo de confianza. ");
        report.Append("Una métrica ausente conserva null; consultar denominadores. Gini por superficie usa exposiciones unidas.\n\n");
        report.Append("| Ámbito | Métrica | Pares disponibles | Proximity | Candidata | Delta | DE delta |\n| --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var row in rows)
            report.Append($"| {row.Scope} | {row.Metric} | {row.AvailablePairs}/{row.SeedCount} | {F(row.BaselineMean)} | {F(row.CandidateMean)} | {F(row.DeltaMean)} | {F(row.DeltaStandardDeviation)} |\n");
        report.Append("\n| Semilla | Variante | Ámbito | Historial | Seleccionadas | nDCG útil | Ideal cero | Vacías seleccionadas | Juicios faltantes |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var run in runs)
            foreach (var (scope, m) in run.Scopes)
                report.Append($"| {run.Seed} | {run.Variant} | {scope} | {m.Metrics.Queries} | {m.SelectedQueries} | {m.Metrics.JudgedQueries} | {m.ZeroIdealQueries} | {m.EmptySelectedQueries} | {m.MissingJudgmentPairs} |\n");
        report.Append("\nExposición modelada sobre todas las consultas; nDCG@10 únicamente en la muestra humana con ideal positivo. ");
        report.Append("Historial móvil de 30 días y distribución acumulada durante los 60 días. No mide ventas ni impresiones reales.\n");
        return report.ToString();
    }
}
