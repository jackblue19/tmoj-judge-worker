using Application.UseCases.Score.Dtos;
using Domain.Entities;

namespace Application.UseCases.Score.Helpers;

/// <summary>
/// Pure functions tính điểm IOI / ACM. Không phụ thuộc DB context, chỉ nhận
/// dữ liệu đã load sẵn từ handler.
///
/// Quy ước tính điểm trong hệ thống:
/// - Class semester slot (student submission): LUÔN IOI (% test case × Points của slot).
/// - Public problem (user submission tự do): LUÔN IOI.
/// - Contest: IOI hoặc ACM, đọc từ Contest.ContestType ("ioi" / "acm").
/// </summary>
public static class ScoringHelper
{
    public const int AcmPenaltyPerWrong = 20;
    public const string AcmPenaltyFormula = "timeOfAc + wrongAttempts * 20";

    private static readonly HashSet<string> WrongVerdicts = ["wa", "tle", "mle", "re"];

    /// <summary>
    /// Tính điểm IOI cho 1 submission. Mỗi test case AC = 1 điểm.
    /// </summary>
    public static (int TotalScore, int PassedCases, int TotalCases, List<IoiCaseDto> Cases) CalcIoiScore(
        IReadOnlyCollection<Result> results,
        IReadOnlyDictionary<Guid, int> ordinalByTestcaseId)
    {
        if (results.Count == 0)
            return (0, 0, 0, new List<IoiCaseDto>());

        var ordered = results
            .Select(r => new
            {
                Result = r,
                Ordinal = r.TestcaseId.HasValue && ordinalByTestcaseId.TryGetValue(r.TestcaseId.Value, out var o)
                    ? (int?)o
                    : null
            })
            .OrderBy(x => x.Ordinal ?? int.MaxValue)
            .ToList();

        int passedCases = ordered.Count(x => x.Result.StatusCode == "ac");

        var cases = ordered.Select(x => new IoiCaseDto(
            TestcaseId: x.Result.TestcaseId,
            Ordinal: x.Ordinal,
            Verdict: x.Result.StatusCode,
            Passed: x.Result.StatusCode == "ac",
            RuntimeMs: x.Result.RuntimeMs,
            MemoryKb: x.Result.MemoryKb,
            Type: x.Result.Type)).ToList();

        return (passedCases, passedCases, ordered.Count, cases);
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
