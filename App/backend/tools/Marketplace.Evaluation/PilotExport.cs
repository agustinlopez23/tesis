using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marketplace.BLL;

namespace Marketplace.Evaluation;

public static class PilotExport
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() }, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    public static void Write(string directory, IReadOnlyList<int> seeds, int queryCount, RankingConfiguration configuration,
        bool ablations = false)
    {
        RankingVariant[] variants = ablations
            ? [RankingVariant.Proximity, RankingVariant.EquityOnly, RankingVariant.EquityNewcomer, RankingVariant.EquityRotation, RankingVariant.Equity]
            : [RankingVariant.Relevance, RankingVariant.Proximity, RankingVariant.Equity, RankingVariant.EquityActivity];
        if (seeds.Count == 0 || seeds.Distinct().Count() != seeds.Count || queryCount is < 1 or > 100000)
            throw new ArgumentException("Provide distinct seeds and 1..100000 queries.");
        _ = new BusinessRanker(configuration);
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new IOException("Output directory must be empty; previous evidence will not be overwritten.");
        Directory.CreateDirectory(directory);
        var runs = new List<PilotRun>();
        var artifacts = new SortedDictionary<string, string>(StringComparer.Ordinal);
        void Register(string name)
        {
            using var input = File.OpenRead(Path.Combine(directory, name));
            artifacts[name] = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        }
        File.WriteAllText(Path.Combine(directory, "configuration.json"), JsonSerializer.Serialize(configuration, Json), new UTF8Encoding(false));
        Register("configuration.json");
        foreach (var seed in seeds)
        {
            var dataset = PilotGenerator.Generate(seed, queryCount);
            var inputName = $"inputs-{seed}.json"; var traceName = $"traces-{seed}.jsonl.gz";
            File.WriteAllText(Path.Combine(directory, inputName), JsonSerializer.Serialize(dataset, Json), new UTF8Encoding(false));
            using (var compressed = new GZipStream(File.Create(Path.Combine(directory, traceName)), CompressionLevel.Fastest))
            using (var writer = new StreamWriter(compressed, new UTF8Encoding(false)) { NewLine = "\n" })
                foreach (var variant in variants)
                    runs.Add(PilotRunner.Run(dataset, configuration, variant,
                        trace => writer.WriteLine(JsonSerializer.Serialize(trace, Json))));
            Register(inputName); Register(traceName);
        }
        var summary = new { schema = ablations ? "ranking-ablation-report-v1" : "ranking-pilot-report-v1", synthetic = true, configuration, seeds, queryCount,
            businessesPerSeed = 200, simulationDays = 60, runs, artifacts };
        File.WriteAllText(Path.Combine(directory, "summary.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions(Json) { WriteIndented = true })
            .Replace("\r\n", "\n"), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "report.md"), Report(runs, seeds, queryCount, configuration), new UTF8Encoding(false));
        Register("summary.json"); Register("report.md");
        File.WriteAllText(Path.Combine(directory, "sha256.json"), JsonSerializer.Serialize(artifacts, Json), new UTF8Encoding(false));
    }

    private static string Report(List<PilotRun> runs, IReadOnlyList<int> seeds, int queries, RankingConfiguration configuration)
    {
        var output = new StringBuilder();
        var ablations = runs.Any(x => x.Variant == RankingVariant.EquityOnly);
        output.Append(ablations ? "# Ablaciones sintéticas de ranking de negocios\n\n" : "# Piloto sintético de ranking de negocios\n\n");
        output.Append(CultureInfo.InvariantCulture, $"Configuración `{configuration.Version}`. 200 negocios y {queries} consultas durante 60 días por semilla: {string.Join(", ", seeds)}.\n\n");
        output.Append("Media ± desviación estándar muestral entre semillas; no es un intervalo de confianza. ");
        output.Append("Sin umbral de aceptación: estos resultados no validan la hipótesis ni prueban ventas.\n\n");
        output.Append("Exposición modelada en top 10, distinta de impresiones visibles reales. ");
        output.Append("Gini y desviación por mérito: menor valor indica menor desigualdad según esa métrica. ");
        output.Append("Cobertura y nDCG: mayor valor. La relevancia se juzga mediante un oráculo sintético independiente del puntaje.\n\n");
        foreach (var stratum in runs.SelectMany(x => x.Strata.Keys).Distinct().Order(StringComparer.Ordinal))
        {
            output.Append($"## {StratumLabel(stratum)}\n\n");
            output.Append("| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |\n");
            output.Append("| --- | --- | --- | --- | --- | --- | --- |\n");
            foreach (var variant in runs.Select(x => x.Variant).Distinct())
            {
                var values = runs.Where(x => x.Variant == variant && x.Strata.ContainsKey(stratum)).Select(x => x.Strata[stratum]).ToArray();
                output.Append($"| {Label(variant)} | {Stats(values.Select(x => x.Distribution.Gini))} | {Stats(values.Select(x => x.Distribution.TopTenShare))} | {Stats(values.Select(x => x.Distribution.Coverage))} | {Stats(values.Select(x => x.Ndcg))} | {Stats(values.Select(x => x.Distribution.MeritDeviation))} | {Stats(values.Select(x => x.NewcomerCoverage))} |\n");
            }
            output.Append('\n');
        }
        output.Append("## Corridas individuales: global\n\n| Semilla | Variante | Consultas | Vacías | Con juicio útil | Elegibles | Gini | nDCG |\n| --- | --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var run in runs)
        {
            var m = run.Strata["all"];
            output.Append(CultureInfo.InvariantCulture, $"| {run.Seed} | {Label(run.Variant)} | {m.Queries} | {m.EmptyQueries} | {m.JudgedQueries} | {m.Distribution.Eligible} | {Format(m.Distribution.Gini)} | {Format(m.Ndcg)} |\n");
        }
        output.Append("\n## Lectura y límites\n\n");
        output.Append("El agregado mezcla oportunidades de zonas y rubros: revisar también cada estrato. ");
        output.Append("El historial del motor expira a 30 días; las métricas acumulan los 60 días de simulación. ");
        output.Append("Nuevos: negocios publicados después del inicio que llegaron a ser elegibles. ");
        output.Append(ablations
            ? "Las cuatro celdas de compensación separan novedad y rotación; Equidad incluye ambas. Sin novedad, su peso vuelve a mérito. Rotación incluye discretización y desempate. Actividad excluida.\n\n"
            : "La ablación de actividad es exógena y sintética, no una señal real aprobada.\n\n");
        output.Append("No incluye productos, juicios humanos, tráfico real, medición HTTP ni causalidad comercial. ");
        output.Append("El ranking público continúa sin cambios. Inputs, configuración, trazas y hashes acompañan este reporte. ");
        output.Append("Cada traza conserva todos los candidatos elegibles con sus juicios y exposición esperada, más los componentes del top 10.\n");
        return output.ToString();
    }

    private static string Stats(IEnumerable<double?> source)
    {
        var values = source.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        if (values.Length == 0) return "sin datos";
        var mean = values.Average();
        var deviation = values.Length == 1 ? (double?)null : Math.Sqrt(values.Sum(x => Math.Pow(x - mean, 2)) / (values.Length - 1));
        return $"{Format(mean)} ± {Format(deviation)}";
    }
    private static string Format(double? value) => value?.ToString("F4", CultureInfo.InvariantCulture) ?? "sin datos";
    private static string Label(RankingVariant variant) => variant switch
    {
        RankingVariant.Relevance => "Relevancia", RankingVariant.Proximity => "Relevancia + proximidad",
        RankingVariant.Equity => "Equidad", RankingVariant.EquityActivity => "Equidad + actividad",
        RankingVariant.EquityOnly => "Solo compensación", RankingVariant.EquityNewcomer => "Compensación + novedad",
        RankingVariant.EquityRotation => "Compensación + rotación", _ => throw new ArgumentOutOfRangeException(nameof(variant))
    };
    private static string StratumLabel(string stratum)
    {
        if (stratum == "all") return "Global";
        var parts = stratum.Split('/');
        var zone = parts[1] switch { "0" => "Centro", "1" => "Nueva Córdoba", _ => "Alberdi" };
        return $"{(parts[0] == "search" ? "Búsqueda de negocios" : "Revista")} · {zone}";
    }
}
