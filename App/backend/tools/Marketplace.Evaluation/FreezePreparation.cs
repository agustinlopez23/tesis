using System.Text;
using System.Text.Json;

namespace Marketplace.Evaluation;

public sealed record FreezePreparationResult(string ManifestSha256, bool HumanReviewComplete, string? PolicyId);
public static class FreezePreparation
{
    public static FreezePreparationResult Write(string sourceRoot, string package, string digest, string output)
    {
        FreezeFiles.Output(output, [package, .. FreezeFiles.Projects.Select(p => Path.Combine(sourceRoot, p)),
            Path.Combine(sourceRoot, "Doc/evaluation/business-ranking-final")]);
        _ = FreezeFiles.Tree(package).ToArray();
        var verified = ReviewPackage.Read(package, digest);
        if (verified.Split != "calibration") throw new ArgumentException("Freeze preparation requires a calibration package.");
        var sources = FreezeFiles.Sources(sourceRoot);
        var sourceHashes = new SortedDictionary<string, string>(sources.ToDictionary(x => x.Key, x => ReviewCases.Hash(x.Value)), StringComparer.Ordinal);
        var sourceTreeSha256 = ReviewCases.Hash(JsonSerializer.SerializeToUtf8Bytes(sourceHashes, PilotExport.Json));
        var runtimeRoot = Path.GetDirectoryName(typeof(FreezePreparation).Assembly.Location)!;
        var runtimeNames = new[] { "Marketplace.Evaluation.dll", "Marketplace.BLL.dll", "Marketplace.Models.dll", "Marketplace.DTOs.dll",
            "Marketplace.Evaluation.runtimeconfig.json", "Marketplace.Evaluation.deps.json" };
        var runtime = runtimeNames.ToDictionary(name => name, name => File.ReadAllBytes(FreezeFiles.SafePath(runtimeRoot, name)));
        foreach (var (name, bytes) in sources) FreezeFiles.Write(output, "sources/" + name, bytes);
        foreach (var (name, bytes) in runtime) FreezeFiles.Write(output, "runtime/" + name, bytes);
        foreach (var (name, bytes) in verified.OriginalFiles) FreezeFiles.Write(output, "review-package/" + name, bytes);
        var complete = CalibrationExport.Write(Path.Combine(output, "review-package"), digest, Path.Combine(output, "calibration"));
        CalibrationPolicy? policy = null;
        if (complete)
        {
            using var summary = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(output, "calibration/summary.json")));
            var id = summary.RootElement.GetProperty("selection").GetProperty("policyId").GetString();
            policy = CalibrationGrid.Policies().SingleOrDefault(p => p.Id == id);
        }
        var pending = new List<string> { "human-training-record", "assessor-authorship-and-dates", "methodological-freeze",
            "source-build-correspondence", "reserved-material-and-custody", "final-evaluator" };
        if (!complete) pending.Insert(0, "human-annotations");
        if (policy is null) pending.Add("eligible-calibration-policy");
        FreezeFiles.Write(output, "LEEME.md", Encoding.UTF8.GetBytes("# Borrador para congelamiento\n\n" +
            "Este archivo conserva evidencia; no congela política ni autoriza evaluación reservada.\n" +
            "Guardar el SHA-256 de manifest.json fuera de esta carpeta. Verificación: --verify-freeze CARPETA --manifest-sha256 HASH --output VACÍO.\n" +
            "Fuentes y binarios están identificados por separado: su correspondencia y el cierre metodológico siguen pendientes.\n" +
            "No se certifican identidad humana, entrenamiento ni fechas de los juicios. La fecha del manifiesto es solo de preparación.\n\n" +
            "Pendientes:\n" + string.Concat(pending.Select(x => "- " + x + "\n"))));
        var hashes = FreezeFiles.Hashes(output);
        HumanRankingExport.WriteJson(output, "manifest.json", new { schema = "business-freeze-draft-v1", status = "draft",
            preparedAtUtc = DateTimeOffset.UtcNow, policyFrozen = false, evaluationAuthorized = false,
            packageSha256 = digest.ToLowerInvariant(), sourceTreeSha256, sourceHashMethod = "sha256-of-ordinal-path-hash-map-json",
            humanReviewComplete = complete, policy, pendingRequirements = pending,
            humanRecords = new { trainingCompletedAt = (string?)null, methodologicalFreezeAt = (string?)null,
                assessorRecords = new[] { new { code = "E1", judgmentsIssuedAt = (string?)null }, new { code = "E2", judgmentsIssuedAt = (string?)null } } },
            files = hashes });
        return new(FreezeFiles.HashFile(Path.Combine(output, "manifest.json")), complete, policy?.Id);
    }
    public static void Verify(string directory, string digest, string output)
    {
        FreezeFiles.Output(output, directory);
        var manifestPath = FreezeFiles.SafePath(directory, "manifest.json");
        var bytes = File.ReadAllBytes(manifestPath);
        if (!string.Equals(ReviewCases.Hash(bytes), digest, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Manifest does not match the externally pinned SHA-256.");
        using var document = JsonDocument.Parse(bytes);
        var manifest = document.RootElement;
        if (manifest.ValueKind != JsonValueKind.Object ||
            !manifest.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.String || schema.GetString() != "business-freeze-draft-v1" ||
            !manifest.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String || status.GetString() != "draft" ||
            !manifest.TryGetProperty("policyFrozen", out var frozen) || frozen.ValueKind != JsonValueKind.False ||
            !manifest.TryGetProperty("evaluationAuthorized", out var authorized) || authorized.ValueKind != JsonValueKind.False ||
            !manifest.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Invalid draft manifest.");
        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in files.EnumerateObject())
        {
            _ = FreezeFiles.SafePath(directory, entry.Name);
            if (entry.Value.ValueKind != JsonValueKind.String || entry.Value.GetString() is not { Length: 64 } hash ||
                hash.Any(c => !char.IsAsciiHexDigit(c)) || !expected.TryAdd(entry.Name, hash) || entry.Name == "manifest.json")
                throw new ArgumentException("Invalid or duplicate artifact hash entry.");
        }
        if (!new[] { "review-package/manifest.json", "runtime/Marketplace.Evaluation.dll",
            "sources/Doc/evaluation/business-ranking-final/protocol.md", "calibration/status.json" }.All(expected.ContainsKey))
            throw new ArgumentException("Draft is missing required evidence.");
        var actual = FreezeFiles.Hashes(directory);
        actual.Remove("manifest.json");
        if (actual.Count != expected.Count || actual.Any(x => !expected.TryGetValue(x.Key, out var hash) ||
            !string.Equals(x.Value, hash, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Draft artifacts were changed, added or removed.");
        Directory.CreateDirectory(output);
        HumanRankingExport.WriteJson(output, "verification.json", new { schema = "business-freeze-verification-v1",
            scope = "integrity-only", status = "verified-draft", manifestSha256 = digest.ToLowerInvariant(),
            verifiedFiles = actual.Count, policyFrozen = false, evaluationAuthorized = false });
    }
}
