using System.Text;
using System.Text.Json;
using Marketplace.Evaluation;
using Microsoft.VisualBasic.FileIO;

namespace Marketplace.BLL.Tests;

public sealed class ReviewPackageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "review-package-" + Guid.NewGuid().ToString("N"));
    private string Package => Path.Combine(root, "package");
    private string Output => Path.Combine(root, "result");
    private static string[][] Rows(byte[] bytes)
    {
        using var reader = new TextFieldParser(new StringReader(Encoding.UTF8.GetString(bytes))) { HasFieldsEnclosedInQuotes = true };
        reader.SetDelimiters(",");
        var rows = new List<string[]>();
        while (!reader.EndOfData) rows.Add(reader.ReadFields()!);
        return rows.ToArray();
    }

    [Theory]
    [InlineData("training", 12, 2)]
    [InlineData("calibration", 30, 5)]
    public void Selection_is_balanced_reproducible_and_all_grades_remain_blank(string split, int count, int perStratum)
    {
        var files = ReviewPackage.Build(split);
        var queries = Rows(files["queries.csv"]).Skip(1).ToArray();
        Assert.Equal(count, queries.Length);
        Assert.All(queries.GroupBy(x => (x[3], x[4])), group => Assert.Equal(perStratum, group.Count()));
        Assert.Equal(6, queries.GroupBy(x => (x[3], x[4])).Count());
        var candidates = Rows(files["candidates.csv"]).Skip(1).ToArray();
        Assert.True(candidates.Length > 20);
        foreach (var row in queries)
        {
            var seed = int.Parse(row[0].Split('/')[^1]);
            var data = JsonSerializer.Deserialize<PilotDataset>(files[$"inputs/{seed}.json"], PilotExport.Json)!;
            var query = data.Queries.Single(x => x.Sequence.ToString() == row[2]);
            var eligible = PilotGenerator.Candidates(data, query).Where(x => x.Published && !x.Suspended &&
                x.PublishedAt <= query.At && x.Relevance >= .2 && x.DistanceKm <= query.RadiusKm).Select(x => x.Id.ToString("D")).Order().ToArray();
            Assert.Equal(eligible, candidates.Where(x => x[0] == row[0] && x[1] == row[2]).Select(x => x[2]).Order());
        }
        foreach (var assessor in new[] { "E1", "E2" })
        {
            var sheet = Rows(files[$"reviewers/{assessor}.csv"]);
            Assert.DoesNotContain("score", sheet[0]);
            Assert.DoesNotContain("oracle", sheet[0]);
            Assert.Equal(candidates.Length, sheet.Length - 1);
            Assert.All(sheet.Skip(1), row => { Assert.Equal("", row[^2]); Assert.Equal("", row[^1]); });
        }
        Assert.NotEqual(Encoding.UTF8.GetString(files["reviewers/E1.csv"]), Encoding.UTF8.GetString(files["reviewers/E2.csv"]));
        var repeated = ReviewPackage.Build(split);
        foreach (var (name, bytes) in files) Assert.Equal(bytes, repeated[name]);
        Assert.Throws<ArgumentException>(() => ReviewPackage.Build("evaluation"));
    }

    [Fact]
    public void Package_round_trip_is_incomplete_until_both_sheets_are_filled()
    {
        var digest = ReviewPackage.Write("training", Package);
        var pending = ReviewPackage.Collect(Package, digest, Output);
        Assert.False(pending.Complete);
        Assert.Equal(pending.RequiredPairs * 2, pending.MissingJudgments);
        foreach (var assessor in new[] { "E1", "E2" })
        {
            var path = Path.Combine(Package, "reviewers", assessor + ".csv");
            // Software fixture only; these are not collected human assessments.
            File.WriteAllText(path, File.ReadAllText(path).Replace(",\"\",\"\"\n", ",\"2\",\"test-fixture-only\"\n"));
        }
        var complete = ReviewPackage.Collect(Package, digest, Path.Combine(root, "complete"));
        Assert.True(complete.Complete);
        using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "complete", "judgments-final.json")));
        Assert.Equal(digest, result.RootElement.GetProperty("packageSha256").GetString());
        var sheetBytes = File.ReadAllBytes(Path.Combine(Package, "reviewers/E1.csv"));
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(sheetBytes)).ToLowerInvariant(),
            result.RootElement.GetProperty("reviewerSha256").GetProperty("reviewers/E1.csv").GetString());
        Assert.Throws<IOException>(() => ReviewPackage.Write("training", Package));
    }

    [Theory]
    [InlineData("candidates.csv")]
    [InlineData("queries.csv")]
    [InlineData("inputs/61.json")]
    [InlineData("reviewers/E1.csv")]
    [InlineData("reviewers/LEEME.txt")]
    public void Changed_facts_or_source_are_rejected_before_output(string file)
    {
        var digest = ReviewPackage.Write("training", Package);
        File.AppendAllText(Path.Combine(Package, file), "tampered\n");
        Assert.Throws<ArgumentException>(() => ReviewPackage.Collect(Package, digest, Output));
        Assert.False(Directory.Exists(Output));
    }

    [Fact]
    public void Manifest_requires_the_previously_pinned_digest()
    {
        var digest = ReviewPackage.Write("training", Package);
        Assert.Throws<ArgumentException>(() => ReviewPackage.Collect(Package, new string('0', 64), Output));
        File.AppendAllText(Path.Combine(Package, "manifest.json"), " ");
        Assert.Throws<ArgumentException>(() => ReviewPackage.Collect(Package, digest, Output));
        Assert.False(Directory.Exists(Output));
    }

    [Fact]
    public void Removing_a_response_stays_incomplete_and_changing_a_fact_in_a_valid_row_fails()
    {
        var digest = ReviewPackage.Write("training", Package);
        var path = Path.Combine(Package, "reviewers", "E1.csv");
        var original = File.ReadAllText(path);
        File.WriteAllText(path, original[..(original.IndexOf('\n') + 1)]);
        var result = ReviewPackage.Collect(Package, digest, Output);
        Assert.False(result.Complete);
        Assert.Contains(result.Issues, x => x.Kind == "missing-row" && x.AssessorId == "E1");
        File.WriteAllText(path, original.Replace("Negocio ficticio.", "Negocio real."));
        Assert.Throws<ArgumentException>(() => ReviewPackage.Collect(Package, digest, Path.Combine(root, "invalid")));
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
