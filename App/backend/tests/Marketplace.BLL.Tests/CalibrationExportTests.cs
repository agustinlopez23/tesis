using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class CalibrationExportTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "calibration-grid-" + Guid.NewGuid().ToString("N"));
    [Fact]
    public void Blank_reviews_are_pending_and_training_or_overwrite_are_rejected()
    {
        var package = Path.Combine(root, "package"); var output = Path.Combine(root, "pending");
        var digest = ReviewPackage.Write("calibration", package);
        Assert.False(CalibrationExport.Write(package, digest, output));
        Assert.False(File.Exists(Path.Combine(output, "summary.json")));
        Assert.Empty(Directory.GetFiles(output, "*.gz"));
        Assert.Throws<IOException>(() => CalibrationExport.Write(package, digest, output));
        var training = Path.Combine(root, "training");
        var trainingDigest = ReviewPackage.Write("training", training);
        Assert.Throws<ArgumentException>(() => CalibrationExport.Write(training, trainingDigest, Path.Combine(root, "invalid")));
        Assert.False(Directory.Exists(Path.Combine(root, "invalid")));
    }
    [Fact]
    public void Software_fixture_runs_full_grid_with_baseline_once_and_verifiable_artifacts()
    {
        var package = Path.Combine(root, "fixture-not-human-evidence"); var output = Path.Combine(root, "grid");
        var digest = ReviewPackage.Write("calibration", package);
        foreach (var assessor in new[] { "E1", "E2" })
        {
            var file = Path.Combine(package, "reviewers", assessor + ".csv");
            File.WriteAllText(file, File.ReadAllText(file).Replace(",\"\",\"\"\n", ",\"2\",\"software-fixture-only\"\n"));
        }
        var draft = Path.Combine(root, "draft");
        var prepared = FreezePreparation.Write(FreezePreparationTests.Workspace(), package, digest, draft);
        Assert.True(prepared.HumanReviewComplete);
        output = Path.Combine(draft, "calibration");
        FreezePreparation.Verify(draft, prepared.ManifestSha256, Path.Combine(root, "verified"));
        var recollected = ReviewPackage.Collect(Path.Combine(draft, "review-package"), digest, Path.Combine(root, "recollected"));
        Assert.True(recollected.Complete);
        using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "summary.json")));
        var doc = summary.RootElement;
        Assert.Equal(digest, doc.GetProperty("packageSha256").GetString());
        Assert.False(doc.GetProperty("finalAcceptanceEvaluated").GetBoolean());
        Assert.True(doc.GetProperty("selection").GetProperty("provisional").GetBoolean());
        Assert.Equal(doc.GetProperty("selection").GetProperty("policyId").GetString(), prepared.PolicyId);
        var candidates = doc.GetProperty("candidates").EnumerateArray().ToArray();
        Assert.Equal(18, candidates.Length);
        Assert.All(candidates, c => {
            Assert.Equal(6, c.GetProperty("runs").GetArrayLength());
            Assert.Equal(54, c.GetProperty("comparisons").GetArrayLength());
            Assert.NotEqual("inconclusive", c.GetProperty("assessment").GetProperty("status").GetString());
        });
        var counts = new Dictionary<string, int>();
        foreach (var file in Directory.GetFiles(output, "traces-*.jsonl.gz"))
        {
            using var gzip = new GZipStream(File.OpenRead(file), CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            while (reader.ReadLine() is { } line)
            {
                using var row = JsonDocument.Parse(line);
                var id = row.RootElement.GetProperty("policyId").GetString()!;
                counts[id] = counts.GetValueOrDefault(id) + 1;
            }
        }
        Assert.Equal(19, counts.Count); Assert.All(counts.Values, count => Assert.Equal(15000, count));
        using var hashes = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "sha256.json")));
        foreach (var artifact in hashes.RootElement.EnumerateObject())
        {
            using var stream = File.OpenRead(Path.Combine(output, artifact.Name));
            Assert.Equal(artifact.Value.GetString(), Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant());
        }
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
