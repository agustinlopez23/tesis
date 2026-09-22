using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class PilotExportTests
{
    [Fact]
    public void Configuration_rejects_misspelled_parameters_instead_of_using_silent_defaults()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Marketplace.BLL.RankingConfiguration>(
            "{\"equityWeght\":0.3}", PilotExport.Json));
    }

    [Fact]
    public void Exports_reproduce_byte_for_byte_and_include_replayable_inputs_and_auditable_traces()
    {
        var root = Path.Combine(Path.GetTempPath(), "ranking-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = Path.Combine(root, "a"); var second = Path.Combine(root, "b");
            PilotExport.Write(first, [17, 29], 12, new());
            PilotExport.Write(second, [17, 29], 12, new());
            var files = Directory.GetFiles(first).Order().ToArray();
            Assert.Contains(files, x => x.EndsWith("report.md"));
            Assert.Contains(files, x => x.EndsWith("summary.json"));
            foreach (var path in files)
                Assert.Equal(SHA256.HashData(File.ReadAllBytes(path)), SHA256.HashData(File.ReadAllBytes(Path.Combine(second, Path.GetFileName(path)))));
            using var stream = new GZipStream(File.OpenRead(Path.Combine(first, "traces-17.jsonl.gz")), CompressionMode.Decompress);
            using var reader = new StreamReader(stream);
            var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(48, lines.Length);
            using var trace = JsonDocument.Parse(lines[0]);
            Assert.True(trace.RootElement.GetProperty("expected").EnumerateObject().Any());
            Assert.True(trace.RootElement.GetProperty("judgments").EnumerateObject().Any());
            Assert.Throws<IOException>(() => PilotExport.Write(first, [17], 12, new()));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
