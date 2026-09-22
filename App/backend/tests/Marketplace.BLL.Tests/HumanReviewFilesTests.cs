using System.Text.Json;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class HumanReviewFilesTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "human-review-test-" + Guid.NewGuid().ToString("N"));
    private string Input => Path.Combine(root, "input");
    private string Output => Path.Combine(root, "output");
    private const string QueryHeader = "dataset_version,split,query_id,surface,zone,category,need,radius_km,constraints\n";
    private const string CandidateHeader = "dataset_version,query_id,business_id,offer_facts,distance_km,limitations,evidence_ref\n";
    private const string JudgmentHeader = "dataset_version,query_id,business_id,assessor_id,grade,reason,rubric_version\n";
    private const string ResolutionHeader = "dataset_version,query_id,business_id,assessor_1_id,grade_1,assessor_2_id,grade_2,final_grade,status,resolver_id,reason,rubric_version\n";
    public HumanReviewFilesTests()
    {
        Directory.CreateDirectory(Input);
        Set("queries.csv", QueryHeader + "cal/101,calibration,q1,search,Centro,Cafés,Café para llevar,4,\ncal/101,calibration,empty,search,Centro,Cafés,Sin oferta,4,\n");
        Set("candidates.csv", CandidateHeader + "cal/101,q1,b1,\"Café, tostadas\nPara llevar\",1,,fixture-only\n");
        Set("judgments.csv", JudgmentHeader + "cal/101,q1,b1,E1,2,,business-relevance-v1\ncal/101,q1,b1,E2,2,,business-relevance-v1\n");
        Set("resolutions.csv", ResolutionHeader);
    }
    private void Set(string file, string contents) => File.WriteAllText(Path.Combine(Input, file), contents);

    [Fact]
    public void Quoted_multiline_utf8_files_produce_auditable_output_and_preserve_empty_queries()
    {
        Assert.True(HumanReviewFiles.Write(Input, Output).Complete);
        using var report = JsonDocument.Parse(File.ReadAllText(Path.Combine(Output, "review-status.json")));
        Assert.Equal(2, report.RootElement.GetProperty("queryCount").GetInt32());
        Assert.Equal(1, report.RootElement.GetProperty("emptyQueries").GetInt32());
        Assert.Equal(4, report.RootElement.GetProperty("inputSha256").EnumerateObject().Count());
        Assert.True(File.Exists(Path.Combine(Output, "judgments-final.json")));
        Assert.Throws<IOException>(() => HumanReviewFiles.Write(Input, Output));
    }

    [Fact]
    public void Unstarted_review_exports_status_but_no_final_judgments()
    {
        Set("judgments.csv", JudgmentHeader + "cal/101,q1,b1,E1,,,business-relevance-v1\n");
        Assert.False(HumanReviewFiles.Write(Input, Output).Complete);
        Assert.False(File.Exists(Path.Combine(Output, "judgments-final.json")));
        Assert.True(File.Exists(Path.Combine(Output, "review-status.json")));
    }

    [Theory]
    [InlineData("judgments.csv", "bad-header\n")]
    [InlineData("judgments.csv", JudgmentHeader + "cal/101,q1,b1,E1,1.5,reason,business-relevance-v1\n")]
    [InlineData("judgments.csv", JudgmentHeader + "cal/101,q1,b1,E1,2,\"unclosed\n")]
    [InlineData("candidates.csv", CandidateHeader + "cal/101,unknown,b1,Offer,1,,fixture\n")]
    [InlineData("candidates.csv", CandidateHeader + "cal/101,q1,b1,Offer,NaN,,fixture\n")]
    [InlineData("queries.csv", QueryHeader + "cal/101,calibration,q1,search,Centro,Cafés,Need,4,\ncal/101,evaluation,q2,search,Centro,Cafés,Need,4,\n")]
    [InlineData("queries.csv", QueryHeader + "cal/101,calibration,q1,search,Centro,Cafés,Need,4,\ncal/101,calibration,q1,search,Centro,Cafés,Need,4,\n")]
    public void Invalid_files_fail_before_creating_output(string file, string contents)
    {
        Set(file, contents);
        Assert.Throws<ArgumentException>(() => HumanReviewFiles.Write(Input, Output));
        Assert.False(Directory.Exists(Output));
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
