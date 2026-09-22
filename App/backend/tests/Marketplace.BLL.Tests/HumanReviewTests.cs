using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class HumanReviewTests
{
    private static readonly ReviewPair Pair = new("calibration/101", "query-1", "business-1");
    private static HumanJudgment Judgment(string assessor, int? grade = 2) => new(Pair, assessor, grade, "Ficha de prueba", HumanReview.Rubric);
    private static HumanResolution Resolution(int first = 2, int second = 3, int final = 2) =>
        new(Pair, "E1", first, "E2", second, final, "adjudicated", "E3", "Resolución de prueba", HumanReview.Rubric);

    [Fact]
    public void Agreement_is_measured_before_adjudication_and_all_candidates_are_required()
    {
        var other = Pair with { BusinessId = "business-2" };
        var result = HumanReview.Validate([Pair, other], [Judgment("E1"), Judgment("E2", 3),
            Judgment("E1", 0) with { Pair = other }, Judgment("E2", 0) with { Pair = other }], [Resolution()]);
        Assert.True(result.Complete);
        Assert.Equal(2, result.ComparedPairs);
        Assert.Equal(.5, result.ExactAgreement);
        Assert.Equal(1, result.AgreementMatrix[2][3]);
        Assert.Equal(1, result.AgreementMatrix[0][0]);
        Assert.Equal(new[] { 2, 0 }, result.FinalJudgments.Select(x => x.Grade));
    }

    [Fact]
    public void Missing_rows_blank_grades_and_disagreements_never_become_zero_or_partial_success()
    {
        var missing = HumanReview.Validate([Pair], [Judgment("E1", null)], []);
        Assert.False(missing.Complete);
        Assert.Equal(2, missing.MissingJudgments);
        Assert.Null(missing.ExactAgreement);
        Assert.Empty(missing.FinalJudgments);
        var conflict = HumanReview.Validate([Pair], [Judgment("E1"), Judgment("E2", 3)], []);
        Assert.False(conflict.Complete);
        Assert.Equal(1, conflict.UnresolvedDisagreements);
        Assert.Empty(conflict.FinalJudgments);
        Assert.False(HumanReview.Validate([], [], []).Complete);
    }

    [Fact]
    public void Pending_work_identifies_missing_rows_blank_grades_and_pairs_needing_adjudication()
    {
        var second = Pair with { BusinessId = "business-2" };
        var result = HumanReview.Validate([Pair, second], [Judgment("E1", null),
            Judgment("E1", 2) with { Pair = second }, Judgment("E2", 3) with { Pair = second }], []);
        Assert.Equal(new[] { new HumanReviewIssue(Pair, "blank-grade", "E1"),
            new HumanReviewIssue(Pair, "missing-row", "E2"), new HumanReviewIssue(second, "needs-adjudication", null) }, result.Issues);
    }

    [Fact]
    public void Duplicate_foreign_wrong_rubric_or_invalid_judgments_are_rejected()
    {
        var valid = Judgment("E1");
        foreach (var invalid in new[] { valid with { AssessorId = "E3" }, valid with { Grade = -1 },
            valid with { Grade = 4 }, valid with { RubricVersion = "other" },
            valid with { Pair = Pair with { DatasetVersion = "evaluation/211" } },
            valid with { Pair = Pair with { BusinessId = "unknown" } }, valid with { Grade = 1, Reason = "" } })
            Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair], [invalid], []));
        Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair], [valid, valid], []));
        Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair, Pair], [], []));
        Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair], [], [], "E1", "E1"));
    }

    [Fact]
    public void Resolutions_must_preserve_original_grades_assessors_and_explain_disagreement()
    {
        HumanJudgment[] judgments = [Judgment("E1"), Judgment("E2", 3)];
        var valid = Resolution();
        foreach (var invalid in new[] { valid with { Grade1 = 1 }, valid with { Assessor2Id = "E1" },
            valid with { FinalGrade = 4 }, valid with { Status = "agreed" }, valid with { Reason = "" },
            valid with { ResolverId = "" }, valid with { RubricVersion = "other" },
            valid with { Pair = Pair with { QueryId = "unknown" } } })
            Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair], judgments, [invalid]));
        Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair], judgments, [valid, valid]));
        Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair], [Judgment("E1")], [valid]));
        Assert.Throws<ArgumentException>(() => HumanReview.Validate([Pair], [Judgment("E1"), Judgment("E2")],
            [Resolution(2, 2, 1) with { Status = "agreed" }]));
    }
}
