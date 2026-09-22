using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace Marketplace.Evaluation;

public static class HumanReviewFiles
{
    public static HumanReviewResult Write(string inputDirectory, string outputDirectory)
        => WriteFiles(new[] { "queries.csv", "candidates.csv", "judgments.csv", "resolutions.csv" }
            .ToDictionary(name => name, name => File.ReadAllBytes(Path.Combine(inputDirectory, name))), outputDirectory);

    internal static HumanReviewResult WriteFiles(IReadOnlyDictionary<string, byte[]> files, string outputDirectory,
        string? packageSha256 = null, IReadOnlyDictionary<string, string>? reviewerSha256 = null)
    {
        if (Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
            throw new IOException("Output directory must be empty; review evidence will not be overwritten.");
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        List<Dictionary<string, string>> Read(string name, string header)
        {
            var bytes = files[name];
            hashes[name] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            return Parse(bytes, name, header);
        }
        var queries = Read("queries.csv", "dataset_version,split,query_id,surface,zone,category,need,radius_km,constraints");
        var candidates = Read("candidates.csv", "dataset_version,query_id,business_id,offer_facts,distance_km,limitations,evidence_ref");
        var judgments = Read("judgments.csv", "dataset_version,query_id,business_id,assessor_id,grade,reason,rubric_version");
        var resolutions = Read("resolutions.csv", "dataset_version,query_id,business_id,assessor_1_id,grade_1,assessor_2_id,grade_2,final_grade,status,resolver_id,reason,rubric_version");
        var queryRadii = new Dictionary<(string, string), double>();
        foreach (var row in queries)
        {
            var radius = Number(row["radius_km"]);
            if (row["split"] is not ("training" or "calibration" or "evaluation") ||
                row["surface"] is not ("search" or "magazine") || radius is < .1 or > 50 ||
                new[] { "zone", "category", "need" }.Any(key => string.IsNullOrWhiteSpace(row[key])) ||
                !queryRadii.TryAdd((Id(row, "dataset_version"), Id(row, "query_id")), radius))
                throw new ArgumentException("Invalid or duplicate query metadata.");
        }
        if (queries.Select(x => x["split"]).Distinct().Count() > 1)
            throw new ArgumentException("Review folders must contain exactly one split, never mixed calibration/evaluation.");
        var pairs = new List<ReviewPair>();
        foreach (var row in candidates)
        {
            var pair = Pair(row); var distance = Number(row["distance_km"]);
            if (!queryRadii.TryGetValue((pair.DatasetVersion, pair.QueryId), out var radius) || distance < 0 || distance > radius ||
                string.IsNullOrWhiteSpace(row["offer_facts"]) || string.IsNullOrWhiteSpace(row["evidence_ref"]))
                throw new ArgumentException("Candidate is outside the query universe or lacks offer evidence.");
            pairs.Add(pair);
        }
        var result = HumanReview.Validate(pairs,
            judgments.Select(row => new HumanJudgment(Pair(row), row["assessor_id"], Grade(row["grade"]), row["reason"], row["rubric_version"])).ToArray(),
            resolutions.Select(row => new HumanResolution(Pair(row), row["assessor_1_id"], RequiredGrade(row["grade_1"]),
                row["assessor_2_id"], RequiredGrade(row["grade_2"]), RequiredGrade(row["final_grade"]),
                row["status"], row["resolver_id"], row["reason"], row["rubric_version"])).ToArray());
        var nonempty = pairs.Select(x => (x.DatasetVersion, x.QueryId)).ToHashSet();
        var report = new { schema = "business-human-review-report-v1", scope = "coverage-and-consistency-only",
            status = result.Complete ? "complete" : "incomplete", rubricVersion = HumanReview.Rubric, packageSha256, reviewerSha256,
            split = queries.FirstOrDefault()?.GetValueOrDefault("split"), queryCount = queries.Count,
            emptyQueries = queryRadii.Keys.Count(x => !nonempty.Contains(x)), inputSha256 = hashes, review = result };
        Directory.CreateDirectory(outputDirectory);
        WriteJson(Path.Combine(outputDirectory, "review-status.json"), report);
        if (result.Complete)
            WriteJson(Path.Combine(outputDirectory, "judgments-final.json"), new { schema = "business-human-qrels-v1",
                rubricVersion = HumanReview.Rubric, inputSha256 = hashes, packageSha256, reviewerSha256, judgments = result.FinalJudgments });
        return result;
    }

    internal static List<Dictionary<string, string>> Parse(byte[] bytes, string name, string expectedHeader)
    {
        try
        {
            using var parser = new TextFieldParser(new StringReader(new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF')))
                { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
            parser.SetDelimiters(",");
            var header = parser.ReadFields();
            if (header is null || !header.SequenceEqual(expectedHeader.Split(',')))
                throw new ArgumentException($"Unexpected header in {name}.");
            var result = new List<Dictionary<string, string>>();
            while (!parser.EndOfData)
            {
                var values = parser.ReadFields();
                if (values is null || values.Length != header.Length)
                    throw new ArgumentException($"Unexpected column count in {name}.");
                result.Add(header.Zip(values).ToDictionary(x => x.First, x => x.Second, StringComparer.Ordinal));
            }
            return result;
        }
        catch (Exception error) when (error is MalformedLineException or DecoderFallbackException)
        { throw new ArgumentException($"Invalid UTF-8 or quoted CSV in {name}.", error); }
    }
    private static string Id(Dictionary<string, string> row, string key) =>
        !string.IsNullOrWhiteSpace(row[key]) && row[key] == row[key].Trim() ? row[key] : throw new ArgumentException($"Invalid {key}.");
    private static ReviewPair Pair(Dictionary<string, string> row) => new(Id(row, "dataset_version"), Id(row, "query_id"), Id(row, "business_id"));
    private static int? Grade(string value) => value == "" ? null : value is "0" or "1" or "2" or "3"
        ? int.Parse(value, CultureInfo.InvariantCulture) : throw new ArgumentException("Grade must be blank or an integer from 0 to 3.");
    private static int RequiredGrade(string value) => Grade(value) ?? throw new ArgumentException("Resolution grades cannot be blank.");
    private static double Number(string value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
        && double.IsFinite(number) ? number : throw new ArgumentException("Distance/radius must be a finite decimal using a period.");
    private static void WriteJson<T>(string path, T value)
    {
        using var file = new FileStream(path, FileMode.CreateNew);
        JsonSerializer.Serialize(file, value, new JsonSerializerOptions(PilotExport.Json) { WriteIndented = true });
    }
}
