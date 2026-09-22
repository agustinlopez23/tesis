using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.BLL;

namespace Marketplace.Evaluation;

public static class CalibrationExport
{
    public static bool Write(string package, string digest, string output)
    {
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("Calibration output must be empty; evidence will not be overwritten.");
        var verified = ReviewPackage.Read(package, digest);
        if (verified.Split != "calibration") throw new ArgumentException("Grid search accepts calibration packages only.");
        var packageSha256 = digest.ToLowerInvariant();
        var review = HumanReviewFiles.WriteFiles(verified.Files, Path.Combine(output, "review"), packageSha256, verified.ReviewerHashes);
        var policies = CalibrationGrid.Policies();
        var baselineConfiguration = new RankingConfiguration();
        HumanRankingExport.WriteJson(output, "grid.json", new { criteriaVersion = CalibrationCriteria.Version,
            baseline = RankingVariant.Proximity, baselineConfiguration, policies });
        using var assembly = File.OpenRead(typeof(CalibrationExport).Assembly.Location);
        var evaluatorAssemblySha256 = Convert.ToHexString(SHA256.HashData(assembly)).ToLowerInvariant();
        void Status(string status) => HumanRankingExport.WriteJson(output, "status.json", new {
            schema = "business-calibration-status-v1", status, packageSha256, reviewerSha256 = verified.ReviewerHashes,
            criteriaVersion = CalibrationCriteria.Version, criteriaEvaluated = review.Complete,
            methodologicalStatus = "draft-guardrails-not-frozen", finalAcceptanceEvaluated = false, evaluatorAssemblySha256 });
        if (!review.Complete) { Status("pending-human-review"); HumanRankingExport.WriteHashes(output); return false; }
        var datasets = ReviewCases.Seeds("calibration").Select(seed =>
            JsonSerializer.Deserialize<PilotDataset>(verified.Files[$"inputs/{seed}.json"], PilotExport.Json)!).ToArray();
        var sample = ReviewCases.Select(datasets, "calibration");
        var baseline = new List<HumanRankingRun>();
        var byPolicy = policies.ToDictionary(p => p.Id, _ => new List<HumanRankingRun>());
        foreach (var dataset in datasets)
        {
            var selected = sample.Where(x => x.Data.Seed == dataset.Seed).Select(x => x.Query.Sequence).ToHashSet();
            var judgments = review.FinalJudgments.Where(x => x.Pair.DatasetVersion == ReviewCases.DatasetId(dataset.Seed))
                .GroupBy(x => int.Parse(x.Pair.QueryId, CultureInfo.InvariantCulture))
                .ToDictionary(x => x.Key, x => (IReadOnlyDictionary<Guid, int>)x.ToDictionary(y => Guid.Parse(y.Pair.BusinessId), y => y.Grade));
            using var file = new FileStream(Path.Combine(output, $"traces-{dataset.Seed}.jsonl.gz"), FileMode.CreateNew);
            using var gzip = new GZipStream(file, CompressionLevel.Fastest);
            using var writer = new StreamWriter(gzip, new UTF8Encoding(false)) { NewLine = "\n" };
            HumanRankingRun Run(string policyId, RankingConfiguration config, RankingVariant variant) =>
                HumanRankingRunner.Run(dataset, config, variant, selected, judgments,
                    trace => writer.WriteLine(JsonSerializer.Serialize(new { policyId, trace }, PilotExport.Json)));
            baseline.Add(Run("baseline", baselineConfiguration, RankingVariant.Proximity));
            foreach (var policy in policies)
                byPolicy[policy.Id].Add(Run(policy.Id, policy.Configuration, policy.Variant));
        }
        var candidates = policies.Select(p => CalibrationGrid.Evaluate(p, [.. baseline, .. byPolicy[p.Id]])).ToArray();
        var selection = CalibrationGrid.Select(candidates);
        HumanRankingExport.WriteJson(output, "summary.json", new { schema = "business-calibration-grid-v1", scope = "calibration-only",
            synthetic = true, relevanceSource = "verified-human-review", finalAcceptanceEvaluated = false,
            criteriaVersion = CalibrationCriteria.Version, methodologicalStatus = "draft-guardrails-not-frozen",
            packageSha256, reviewerSha256 = verified.ReviewerHashes, evaluatorAssemblySha256, candidates, selection });
        using (var writer = new StreamWriter(new FileStream(Path.Combine(output, "report.md"), FileMode.CreateNew), new UTF8Encoding(false)))
            writer.Write(Report(candidates, selection));
        Status("completed-calibration-grid"); HumanRankingExport.WriteHashes(output);
        return true;
    }
    private static string Report(IReadOnlyList<CalibrationCandidate> candidates, CalibrationSelection selection)
    {
        var report = new StringBuilder("# Barrido de calibración\n\n");
        report.Append("18 políticas, tres semillas y 5.000 consultas por semilla; referencia ejecutada una vez con historia independiente. ");
        report.Append("Relevancia de hojas verificadas sobre fichas ficticias. No es evaluación reservada ni aprobación de despliegue.\n\n");
        report.Append($"Selección: **{selection.Status}**. Candidata provisional: `{selection.PolicyId ?? "ninguna"}`. Motivo: `{selection.Reason}`.\n\n");
        report.Append("Criterios acordados y salvaguardas propuestas se reportan por separado. La selección usa ambos; el diseño sigue sin congelar. ");
        report.Append("Una celda no concluyente impide seleccionar. Desempate: menor desviación por mérito, mayor nDCG, menor Eq/N, sin rotación.\n\n");
        report.Append("| Política | Criterios acordados | Salvaguardas propuestas | Estado combinado |\n| --- | --- | --- | --- |\n");
        foreach (var c in candidates)
            report.Append($"| {c.Policy.Id} | {c.Assessment.MainStatus} | {c.Assessment.GuardrailStatus} | {c.Assessment.Status} |\n");
        report.Append("\n## Comprobaciones fallidas o faltantes\n\n| Política | Ámbito | Semilla | Regla | Procedencia |\n| --- | --- | --- | --- | --- |\n");
        foreach (var c in candidates)
            foreach (var check in c.Assessment.Checks.Where(x => x.Passed != true))
                report.Append($"| {c.Policy.Id} | {check.Scope} | {check.Seed?.ToString(CultureInfo.InvariantCulture) ?? "media"} | {check.Rule} | {check.Origin} |\n");
        report.Append("\nsummary.json conserva todos los valores por semilla, denominadores, medias/diferencias pareadas y límites efectivos de cada regla. ");
        report.Append("Traces conserva cada consulta con policyId; grid.json identifica sus pesos. Los hashes incluyen respuestas, resoluciones y ensamblado evaluador. ");
        report.Append("Cumplir aquí describe únicamente calibración bajo este diseño provisional. No congela una política ni abre la reserva.\n");
        return report.ToString();
    }
}
