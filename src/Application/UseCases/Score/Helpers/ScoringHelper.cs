using Application.UseCases.Score.Dtos;
using Domain.Entities;

namespace Application.UseCases.Score.Helpers;

/// <summary>
/// Pure functions tính điểm IOI / ACM. Không phụ thuộc DB context, chỉ nhận
/// dữ liệu đã load sẵn từ handler.
///
/// Quy ước tính điểm trong hệ thống:
/// - Mặc định MỌI problem (public & private, standalone submission) đều IOI.
/// - Chỉ có Contest mới có thể chấm theo ACM, căn cứ vào Contest.ContestType == "acm".
/// - ACM ↔ StopOnFirstFail = true (worker dừng ngay ở test case đầu FAIL).
/// - IOI ↔ StopOnFirstFail = false (chấm hết mọi test case, cộng điểm theo Weight).
/// </summary>
public static class ScoringHelper
{
    public const int AcmPenaltyPerWrong = 20;
    public const string AcmPenaltyFormula = "timeOfAc + wrongAttempts * 20";

    private static readonly HashSet<string> WrongVerdicts = ["wa", "tle", "mle", "re"];

    /// <summary>
    /// Contest có dùng ACM hay không. Mọi giá trị khác "acm" (bao gồm null/"ioi"/"class") → IOI.
    /// </summary>
    public static bool IsAcmContest(Contest contest)
        => string.Equals(contest.ContestType?.Trim(), "acm", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Tính điểm IOI cho 1 submission. Mỗi test case AC cộng Weight của chính nó;
    /// test case chưa map được (legacy) coi như Weight = 1.
    /// </summary>
    public static (int TotalScore, int PassedCases, int TotalCases, List<IoiCaseDto> Cases) CalcIoiScore(
        IReadOnlyCollection<Result> results,
        IReadOnlyDictionary<Guid, (int Ordinal, int Weight)> testcaseInfo)
    {
        if (results.Count == 0)
            return (0, 0, 0, new List<IoiCaseDto>());

        var ordered = results
            .Select(r =>
            {
                (int Ordinal, int Weight)? info = r.TestcaseId.HasValue && testcaseInfo.TryGetValue(r.TestcaseId.Value, out var hit)
                    ? hit
                    : null;
                return new
                {
                    Result = r,
                    Ordinal = info?.Ordinal,
                    Weight = info?.Weight ?? 1
                };
            })
            .OrderBy(x => x.Ordinal ?? int.MaxValue)
            .ToList();

        int passedCases = ordered.Count(x => x.Result.StatusCode == "ac");
        int totalScore = ordered
            .Where(x => x.Result.StatusCode == "ac")
            .Sum(x => x.Weight);

        var cases = ordered.Select(x => new IoiCaseDto(
            TestcaseId: x.Result.TestcaseId,
            Ordinal: x.Ordinal,
            Weight: x.Weight,
            Verdict: x.Result.StatusCode,
            Passed: x.Result.StatusCode == "ac",
            RuntimeMs: x.Result.RuntimeMs,
            MemoryKb: x.Result.MemoryKb,
            Type: x.Result.Type)).ToList();

        return (totalScore, passedCases, ordered.Count, cases);
    }

    /// <summary>
    /// Tính điểm ACM cho 1 problem của 1 team:
    /// - Solved khi có submission AC.
    /// - Penalty = phút từ contestStart đến AC đầu + wrongAttempts × 20.
    /// </summary>
    public static AcmProblemResultDto CalcAcmProblem(
        IReadOnlyCollection<Submission> submissions,
        DateTime contestStartAt)
    {
        var ordered = submissions
            .OrderBy(s => s.CreatedAt)
            .ToList();

        var firstAc = ordered.FirstOrDefault(s => s.VerdictCode == "ac");

        var beforeAc = firstAc is null
            ? ordered
            : ordered.Where(s => s.CreatedAt < firstAc.CreatedAt).ToList();

        int wrongAttempts = beforeAc.Count(s =>
            s.VerdictCode != null && WrongVerdicts.Contains(s.VerdictCode));

        int penaltyMinutes = 0;
        if (firstAc is not null)
        {
            var minutesFromStart = (int)Math.Floor((firstAc.CreatedAt - contestStartAt).TotalMinutes);
            penaltyMinutes = minutesFromStart + wrongAttempts * AcmPenaltyPerWrong;
        }

        var history = ordered.Select(s => new AcmSubmissionEntryDto(
            Id: s.Id,
            Verdict: s.VerdictCode,
            SubmittedAt: s.CreatedAt)).ToList();

        return new AcmProblemResultDto(
            Solved: firstAc is not null,
            Score: firstAc is not null ? 1 : 0,
            WrongAttempts: wrongAttempts,
            PenaltyMinutes: penaltyMinutes,
            FirstAcAt: firstAc?.CreatedAt,
            TotalSubmissions: ordered.Count,
            SubmissionHistory: history);
    }
}
