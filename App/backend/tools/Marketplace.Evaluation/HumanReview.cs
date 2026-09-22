namespace Marketplace.Evaluation;

public sealed record ReviewPair(string DatasetVersion, string QueryId, string BusinessId);
public sealed record HumanJudgment(ReviewPair Pair, string AssessorId, int? Grade, string Reason, string RubricVersion);
public sealed record HumanResolution(ReviewPair Pair, string Assessor1Id, int Grade1, string Assessor2Id,
    int Grade2, int FinalGrade, string Status, string ResolverId, string Reason, string RubricVersion);
public sealed record FinalHumanJudgment(ReviewPair Pair, int Grade);
public sealed record HumanReviewIssue(ReviewPair Pair, string Kind, string? AssessorId);
public sealed record HumanReviewResult(bool Complete, int RequiredPairs, int MissingJudgments,
    int UnresolvedDisagreements, int ComparedPairs, double? ExactAgreement, int[][] AgreementMatrix,
    IReadOnlyList<FinalHumanJudgment> FinalJudgments)
{
    public IReadOnlyList<HumanReviewIssue> Issues { get; init; } = [];
}

public static class HumanReview
{
    public const string Rubric = "business-relevance-v1";
    public static HumanReviewResult Validate(IReadOnlyList<ReviewPair> expected,
        IReadOnlyList<HumanJudgment> judgments, IReadOnlyList<HumanResolution> resolutions,
        string assessor1 = "E1", string assessor2 = "E2")
    {
        if (string.IsNullOrWhiteSpace(assessor1) || string.IsNullOrWhiteSpace(assessor2) || assessor1 == assessor2)
            throw new ArgumentException("Two distinct assessor codes are required.");
        var pairs = expected.ToHashSet();
        if (pairs.Count != expected.Count || pairs.Any(x => string.IsNullOrWhiteSpace(x.DatasetVersion) ||
            string.IsNullOrWhiteSpace(x.QueryId) || string.IsNullOrWhiteSpace(x.BusinessId)))
            throw new ArgumentException("Candidate pairs must have unique, nonempty identifiers.");
        var reviews = new Dictionary<(ReviewPair, string), HumanJudgment>();
        foreach (var row in judgments)
            if (!pairs.Contains(row.Pair) || row.AssessorId != assessor1 && row.AssessorId != assessor2 ||
                row.RubricVersion != Rubric || row.Grade is < 0 or > 3 ||
                row.Grade is 0 or 1 && string.IsNullOrWhiteSpace(row.Reason) ||
                !reviews.TryAdd((row.Pair, row.AssessorId), row))
                throw new ArgumentException("Invalid, foreign or duplicate human judgment.");
        var resolved = new Dictionary<ReviewPair, HumanResolution>();
        foreach (var row in resolutions)
        {
            if (!pairs.Contains(row.Pair) || row.RubricVersion != Rubric || row.FinalGrade is < 0 or > 3 ||
                row.Assessor1Id != assessor1 || row.Assessor2Id != assessor2 ||
                !reviews.TryGetValue((row.Pair, assessor1), out var first) || first.Grade is null ||
                !reviews.TryGetValue((row.Pair, assessor2), out var second) || second.Grade is null ||
                row.Grade1 != first.Grade || row.Grade2 != second.Grade || !resolved.TryAdd(row.Pair, row))
                throw new ArgumentException("Resolution must match both original judgments.");
            var agreed = first.Grade == second.Grade;
            if (agreed ? row.Status != "agreed" || row.FinalGrade != first.Grade :
                row.Status != "adjudicated" || string.IsNullOrWhiteSpace(row.Reason) ||
                string.IsNullOrWhiteSpace(row.ResolverId) || row.ResolverId == assessor1 || row.ResolverId == assessor2)
                throw new ArgumentException("Resolution must preserve agreement or explain adjudication by a third assessor or consensus.");
        }
        var matrix = Enumerable.Range(0, 4).Select(_ => new int[4]).ToArray();
        var finals = new List<FinalHumanJudgment>();
        var issues = new List<HumanReviewIssue>();
        var missing = 0; var unresolved = 0; var compared = 0; var agreements = 0;
        foreach (var pair in pairs.OrderBy(x => x.DatasetVersion, StringComparer.Ordinal)
            .ThenBy(x => x.QueryId, StringComparer.Ordinal).ThenBy(x => x.BusinessId, StringComparer.Ordinal))
        {
            var first = reviews.GetValueOrDefault((pair, assessor1))?.Grade;
            var second = reviews.GetValueOrDefault((pair, assessor2))?.Grade;
            missing += (first is null ? 1 : 0) + (second is null ? 1 : 0);
            foreach (var assessor in new[] { assessor1, assessor2 })
                if (reviews.GetValueOrDefault((pair, assessor))?.Grade is null)
                    issues.Add(new(pair, reviews.ContainsKey((pair, assessor)) ? "blank-grade" : "missing-row", assessor));
            if (first is null || second is null) continue;
            compared++; matrix[first.Value][second.Value]++;
            if (first == second) { agreements++; finals.Add(new(pair, first.Value)); }
            else if (resolved.TryGetValue(pair, out var resolution)) finals.Add(new(pair, resolution.FinalGrade));
            else { unresolved++; issues.Add(new(pair, "needs-adjudication", null)); }
        }
        var complete = pairs.Count > 0 && missing == 0 && unresolved == 0;
        return new(complete, pairs.Count, missing, unresolved, compared,
            compared == 0 ? null : (double)agreements / compared, matrix, complete ? finals : []) { Issues = issues };
    }
}
