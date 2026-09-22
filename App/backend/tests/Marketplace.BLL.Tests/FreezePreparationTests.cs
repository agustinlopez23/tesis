using System.Security.Cryptography;
using System.Text.Json;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class FreezePreparationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "freeze-draft-" + Guid.NewGuid().ToString("N"));
    internal static string Workspace()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Doc/PRD.md"))) directory = directory.Parent;
        return directory?.FullName ?? throw new IOException("Repository fixture sources not found.");
    }
    private (string Output, FreezePreparationResult Result) Prepare()
    {
        var package = Path.Combine(root, "package"); var output = Path.Combine(root, "draft");
        var digest = ReviewPackage.Write("calibration", package);
        return (output, FreezePreparation.Write(Workspace(), package, digest, output));
    }
    [Fact]
    public void Blank_reviews_create_self_contained_draft_with_no_policy_or_approval()
    {
        var (output, result) = Prepare();
        Assert.False(result.HumanReviewComplete); Assert.Null(result.PolicyId);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "manifest.json")));
        var manifest = document.RootElement;
        Assert.Equal("draft", manifest.GetProperty("status").GetString());
        Assert.False(manifest.GetProperty("policyFrozen").GetBoolean());
        Assert.False(manifest.GetProperty("evaluationAuthorized").GetBoolean());
        Assert.Null(manifest.GetProperty("policy").Deserialize<object>());
        Assert.Contains("human-annotations", manifest.GetProperty("pendingRequirements").EnumerateArray().Select(x => x.GetString()));
        var paths = manifest.GetProperty("files").EnumerateObject().Select(x => x.Name).ToArray();
        Assert.Contains("sources/App/backend/tools/Marketplace.Evaluation/CalibrationCriteria.cs", paths);
        Assert.Contains("sources/Doc/evaluation/business-ranking-final/protocol.md", paths);
        Assert.Contains("runtime/Marketplace.BLL.dll", paths);
        Assert.DoesNotContain(paths, p => p.Contains("/bin/") || p.Contains("/obj/") || p.Contains(".env") || p.Contains("/.git/"));
        var review = ReviewPackage.Collect(Path.Combine(output, "review-package"), manifest.GetProperty("packageSha256").GetString()!, Path.Combine(root, "recollected"));
        Assert.Equal(2286, review.MissingJudgments);
        Assert.False(File.Exists(Path.Combine(output, "calibration/summary.json")));
        FreezePreparation.Verify(output, result.ManifestSha256, Path.Combine(root, "verified"));
        Assert.Throws<IOException>(() => FreezePreparation.Write(Workspace(), Path.Combine(root, "package"), manifest.GetProperty("packageSha256").GetString()!, output));
    }
    [Theory]
    [InlineData("tamper")]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("pin")]
    public void Verification_rejects_changes_missing_extra_files_or_wrong_external_pin(string mode)
    {
        var (output, result) = Prepare();
        var file = Path.Combine(output, "sources/Doc/evaluation/business-ranking-final/protocol.md");
        if (mode == "tamper") File.AppendAllText(file, "changed");
        if (mode == "missing") File.Delete(file);
        if (mode == "extra") File.WriteAllText(Path.Combine(output, "extra.txt"), "extra");
        Assert.Throws<ArgumentException>(() => FreezePreparation.Verify(output,
            mode == "pin" ? new string('0', 64) : result.ManifestSha256, Path.Combine(root, "invalid")));
        Assert.False(Directory.Exists(Path.Combine(root, "invalid")));
    }
    [Fact]
    public void Even_a_pinned_manifest_cannot_reference_paths_outside_its_directory()
    {
        var output = Path.Combine(root, "malicious"); Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(root, "outside.txt"), "do not read as artifact");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { schema = "business-freeze-draft-v1", status = "draft",
            policyFrozen = false, evaluationAuthorized = false, files = new Dictionary<string, string> { ["../outside.txt"] = new string('0', 64) } });
        File.WriteAllBytes(Path.Combine(output, "manifest.json"), bytes);
        Assert.Throws<ArgumentException>(() => FreezePreparation.Verify(output,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), Path.Combine(root, "invalid")));
    }
    [Fact]
    public void Training_wrong_sources_and_output_inside_input_are_rejected_without_writes()
    {
        var package = Path.Combine(root, "training"); var digest = ReviewPackage.Write("training", package);
        Assert.Throws<ArgumentException>(() => FreezePreparation.Write(Workspace(), package, digest, Path.Combine(root, "invalid")));
        var calibration = Path.Combine(root, "calibration"); digest = ReviewPackage.Write("calibration", calibration);
        Assert.Throws<ArgumentException>(() => FreezePreparation.Write(Workspace(), calibration, digest, Path.Combine(calibration, "nested")));
        Assert.Throws<ArgumentException>(() => FreezePreparation.Write(root, calibration, digest, Path.Combine(root, "invalid")));
        Assert.False(Directory.Exists(Path.Combine(root, "invalid")));
        Assert.False(Directory.Exists(Path.Combine(calibration, "nested")));
    }
    [Fact]
    public void Build_metadata_in_intermediate_directories_is_captured_and_changes_source_fingerprint()
    {
        var (output, first) = Prepare();
        var sources = Path.Combine(output, "sources");
        File.WriteAllText(Path.Combine(sources, "App/backend/src/Directory.Build.props"), "<Project />");
        using var old = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "manifest.json")));
        var second = Path.Combine(root, "second");
        FreezePreparation.Write(sources, Path.Combine(output, "review-package"), old.RootElement.GetProperty("packageSha256").GetString()!, second);
        Assert.True(File.Exists(Path.Combine(second, "sources/App/backend/src/Directory.Build.props")));
        using var current = JsonDocument.Parse(File.ReadAllText(Path.Combine(second, "manifest.json")));
        Assert.NotEqual(old.RootElement.GetProperty("sourceTreeSha256").GetString(), current.RootElement.GetProperty("sourceTreeSha256").GetString());
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
