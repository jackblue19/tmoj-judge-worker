using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/debug/score")]
[Tags("ScoreDebug")]
public class ScoreDebugController : ControllerBase
{
    private readonly TmojDbContext _db;

    public ScoreDebugController(TmojDbContext db)
    {
        _db = db;
    }

    // =============================================
    // IOI SCORING — per submission
    // Mỗi test case pass = 1 điểm. Score = số test case AC.
    // =============================================

    /// <summary>
    /// Tính điểm IOI cho một submission. Mỗi test case pass = 1 điểm.
    /// </summary>
    [HttpGet("ioi/submission/{submissionId:guid}")]
    public async Task<IActionResult> ScoreIoiSubmission(Guid submissionId, CancellationToken ct)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

        if (submission is null)
            return NotFound(new { message = "Submission not found." });

        var score = await CalcIoiScoreAsync(submissionId, ct);

        return Ok(new
        {
            submissionId,
            scoringMode = "ioi",
            verdict = submission.VerdictCode,
            note = submission.VerdictCode == "ce"
                ? "Submission bị Compile Error, không có test case nào được chạy."
                : null,
            score.TotalScore,
            score.PassedCases,
            score.TotalCases,
            score.Cases
        });
    }

    // =============================================
    // IOI SCORING — per problem (nhiều lần nộp, lấy điểm cao nhất)
    // =============================================

    /// <summary>
    /// Tính điểm IOI tốt nhất cho một team/problem trong contest.
    /// Lấy submission có điểm cao nhất trong tất cả các lần nộp.
    /// </summary>
    [HttpGet("ioi/problem/{contestProblemId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreIoiProblem(Guid contestProblemId, Guid teamId, CancellationToken ct)
    {
        var cp = await _db.ContestProblems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contestProblemId, ct);

        if (cp is null)
            return NotFound(new { message = "ContestProblem not found." });

        var submissionIds = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.ContestProblemId == contestProblemId
                        && s.TeamId == teamId
                        && s.StatusCode == "done")
            .OrderBy(s => s.CreatedAt)
            .Select(s => new { s.Id, s.CreatedAt })
            .ToListAsync(ct);

        if (submissionIds.Count == 0)
            return Ok(new
            {
                contestProblemId,
                teamId,
                scoringMode = "ioi",
                bestScore = 0,
                totalSubmissions = 0,
                bestSubmissionId = (Guid?)null,
                submissions = Array.Empty<object>()
            });

        // Tính điểm cho từng submission, lấy điểm cao nhất
        IoiScoreResult? best = null;
        Guid bestSubmissionId = Guid.Empty;
        var allResults = new List<object>();

        foreach (var s in submissionIds)
        {
            var result = await CalcIoiScoreAsync(s.Id, ct);
            allResults.Add(new
            {
                submissionId = s.Id,
                submittedAt = s.CreatedAt,
                result.TotalScore,
                result.PassedCases,
                result.TotalCases
            });

            if (best is null || result.TotalScore > best.TotalScore)
            {
                best = result;
                bestSubmissionId = s.Id;
            }
        }

        return Ok(new
        {
            contestProblemId,
            teamId,
            scoringMode = "ioi",
            bestScore = best!.TotalScore,
            totalSubmissions = submissionIds.Count,
            bestSubmissionId,
            bestSubmissionDetail = new
            {
                best.PassedCases,
                best.TotalCases,
                best.Cases
            },
            submissions = allResults
        });
    }

    // =============================================
    // CONTEST SCORING — auto-detect IOI / ACM từ Contest.ContestType
    // IOI: lấy điểm cao nhất mỗi problem, cộng tổng
    // ACM: đếm solved + cộng dồn penalty
    // =============================================

    /// <summary>
    /// Tính điểm tổng hợp toàn contest cho một team.
    /// Tự động chọn IOI hoặc ACM dựa theo Contest.ContestType.
    /// Problem ngoài contest luôn dùng IOI.
    /// </summary>
    [HttpGet("contest/{contestId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreContest(Guid contestId, Guid teamId, CancellationToken ct)
    {
        var contest = await _db.Contests
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contestId, ct);

        if (contest is null)
            return NotFound(new { message = "Contest not found." });

        var scoringMode = contest.ContestType?.ToLower() == "acm" ? "acm" : "ioi";

        var contestProblems = await _db.ContestProblems
            .AsNoTracking()
            .Where(cp => cp.ContestId == contestId && cp.IsActive)
            .OrderBy(cp => cp.Ordinal)
            .ToListAsync(ct);

        if (scoringMode == "acm")
        {
            int totalScore = 0;
            int totalPenalty = 0;
            var problemResults = new List<object>();

            foreach (var cp in contestProblems)
            {
                var r = await CalcAcmProblemAsync(cp, contest.StartAt, teamId, ct);

                if (r.Solved)
                {
                    totalScore += 1;
                    totalPenalty += r.PenaltyMinutes;
                }

                problemResults.Add(new
                {
                    contestProblemId = cp.Id,
                    alias = cp.Alias,
                    ordinal = cp.Ordinal,
                    r.Solved,
                    score = r.Solved ? 1 : 0,
                    r.WrongAttempts,
                    r.PenaltyMinutes,
                    r.FirstAcAt,
                    r.TotalSubmissions
                });
            }

            return Ok(new
            {
                contestId,
                teamId,
                scoringMode,
                totalScore,
                totalPenalty,
                penaltyFormula = "timeOfAc + wrongAttempts * 20",
                solvedCount = totalScore,
                totalProblems = contestProblems.Count,
                problems = problemResults
            });
        }
        else // ioi
        {
            int totalScore = 0;
            var problemResults = new List<object>();

            foreach (var cp in contestProblems)
            {
                var submissionIds = await _db.Submissions
                    .AsNoTracking()
                    .Where(s => s.ContestProblemId == cp.Id
                                && s.TeamId == teamId
                                && s.StatusCode == "done")
                    .OrderBy(s => s.CreatedAt)
                    .Select(s => new { s.Id, s.CreatedAt })
                    .ToListAsync(ct);

                IoiScoreResult? best = null;
                Guid? bestSubId = null;

                foreach (var s in submissionIds)
                {
                    var r = await CalcIoiScoreAsync(s.Id, ct);
                    if (best is null || r.TotalScore > best.TotalScore)
                    {
                        best = r;
                        bestSubId = s.Id;
                    }
                }

                int problemBestScore = best?.TotalScore ?? 0;
                totalScore += problemBestScore;

                problemResults.Add(new
                {
                    contestProblemId = cp.Id,
                    alias = cp.Alias,
                    ordinal = cp.Ordinal,
                    bestScore = problemBestScore,
                    bestSubmissionId = bestSubId,
                    totalSubmissions = submissionIds.Count,
                    passedCases = best?.PassedCases ?? 0,
                    totalCases = best?.TotalCases ?? 0
                });
            }

            return Ok(new
            {
                contestId,
                teamId,
                scoringMode,
                totalScore,
                totalProblems = contestProblems.Count,
                problems = problemResults
            });
        }
    }

    // =============================================
    // STANDALONE PROBLEM SCORING — luôn dùng IOI
    // Problem không nằm trong contest
    // =============================================

    /// <summary>
    /// Tính điểm IOI cho một submission của problem độc lập (không trong contest).
    /// Mỗi test case pass = 1 điểm.
    /// </summary>
    [HttpGet("problem/{submissionId:guid}")]
    public async Task<IActionResult> ScoreStandaloneProblem(Guid submissionId, CancellationToken ct)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

        if (submission is null)
            return NotFound(new { message = "Submission not found." });

        if (submission.ContestProblemId.HasValue)
            return BadRequest(new { message = "Submission belongs to a contest. Use /contest/{contestId}/{teamId} instead." });

        var score = await CalcIoiScoreAsync(submissionId, ct);

        return Ok(new
        {
            submissionId,
            problemId = submission.ProblemId,
            scoringMode = "ioi",
            score.TotalScore,
            score.PassedCases,
            score.TotalCases,
            score.Cases
        });
    }

    // =============================================
    // ACM SCORING — per problem
    // 1 AC = 1 điểm, penalty cộng dồn qua các problem
    // Penalty = time_of_first_ac_in_minutes + wrongAttemptsBeforeAc * penaltyPerWrong
    // =============================================

    /// <summary>
    /// Tính điểm ACM cho một problem cụ thể của team.
    /// - Solved = 1 điểm, chưa AC = 0 điểm.
    /// - Penalty của problem = phút từ lúc contest bắt đầu đến AC đầu tiên
    ///   + số lần WA/TLE/MLE/RE trước AC * PenaltyPerWrong.
    /// </summary>
    [HttpGet("acm/problem/{contestProblemId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreAcmProblem(Guid contestProblemId, Guid teamId, CancellationToken ct)
    {
        var cp = await _db.ContestProblems
            .AsNoTracking()
            .Include(x => x.Contest)
            .FirstOrDefaultAsync(x => x.Id == contestProblemId, ct);

        if (cp is null)
            return NotFound(new { message = "ContestProblem not found." });

        var result = await CalcAcmProblemAsync(cp, cp.Contest.StartAt, teamId, ct);

        return Ok(new
        {
            contestProblemId,
            teamId,
            scoringMode = "acm",
            penaltyFormula = "timeOfAc + wrongAttempts * 20",
            result.Solved,
            score = result.Solved ? 1 : 0,
            result.WrongAttempts,
            result.PenaltyMinutes,
            result.FirstAcAt,
            result.TotalSubmissions,
            result.SubmissionHistory
        });
    }

    // =============================================
    // ACM SCORING — toàn contest
    // TotalScore = số problem đã AC
    // TotalPenalty = tổng penalty của từng problem đã AC
    // =============================================

    /// <summary>
    /// Tính điểm ACM tổng hợp toàn contest cho một team.
    /// - TotalScore = số problem đã AC.
    /// - TotalPenalty = tổng penalty cộng dồn qua tất cả problem đã AC.
    /// </summary>
    [HttpGet("acm/contest/{contestId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreAcmContest(Guid contestId, Guid teamId, CancellationToken ct)
    {
        var contest = await _db.Contests
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contestId, ct);

        if (contest is null)
            return NotFound(new { message = "Contest not found." });

        var contestProblems = await _db.ContestProblems
            .AsNoTracking()
            .Where(cp => cp.ContestId == contestId && cp.IsActive)
            .OrderBy(cp => cp.Ordinal)
            .ToListAsync(ct);

        int totalScore = 0;
        int totalPenalty = 0;
        var problemResults = new List<object>();

        foreach (var cp in contestProblems)
        {
            var r = await CalcAcmProblemAsync(cp, contest.StartAt, teamId, ct);

            if (r.Solved)
            {
                totalScore += 1;
                totalPenalty += r.PenaltyMinutes;
            }

            problemResults.Add(new
            {
                contestProblemId = cp.Id,
                alias = cp.Alias,
                ordinal = cp.Ordinal,
                r.Solved,
                score = r.Solved ? 1 : 0,
                r.WrongAttempts,
                r.PenaltyMinutes,
                r.FirstAcAt,
                r.TotalSubmissions
            });
        }

        return Ok(new
        {
            contestId,
            teamId,
            scoringMode = "acm",
            totalScore,
            totalPenalty,
            solvedCount = totalScore,
            totalProblems = contestProblems.Count,
            problems = problemResults
        });
    }

    // =============================================
    // DEBUG — xem raw data của submission để biết tại sao điểm = 0
    // =============================================

    /// <summary>
    /// Trả về toàn bộ Result + JudgeRun của submission để debug.
    /// </summary>
    [HttpGet("inspect/{submissionId:guid}")]
    public async Task<IActionResult> InspectSubmission(Guid submissionId, CancellationToken ct)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

        if (submission is null)
            return NotFound(new { message = "Submission not found." });

        var rawResults = await _db.Results
            .AsNoTracking()
            .Where(r => r.SubmissionId == submissionId)
            .Select(r => new
            {
                r.Id,
                r.TestcaseId,
                r.StatusCode,
                r.Type,
                r.RuntimeMs,
                r.MemoryKb,
                r.Message,
                r.JudgeRunId
            })
            .ToListAsync(ct);

        var judgeRuns = await _db.JudgeRuns
            .AsNoTracking()
            .Where(jr => jr.SubmissionId == submissionId)
            .ToListAsync(ct);

        var judgeJobs = await _db.JudgeJobs
            .AsNoTracking()
            .Where(jj => jj.SubmissionId == submissionId)
            .Select(jj => new { jj.Id, jj.Status, jj.LastError, jj.EnqueueAt })
            .ToListAsync(ct);

        return Ok(new
        {
            submission = new
            {
                submission.Id,
                submission.StatusCode,
                submission.VerdictCode,
                submission.FinalScore,
                submission.JudgedAt,
                submission.TestsetId,
                submission.ProblemId,
                submission.ContestProblemId
            },
            resultCount = rawResults.Count,
            results = rawResults,
            judgeRunCount = judgeRuns.Count,
            judgeRuns,
            judgeJobs
        });
    }

    // =============================================
    // Helpers
    // =============================================

    private static readonly HashSet<string> WrongVerdicts = ["wa", "tle", "mle", "re"];

    /// <summary>
    /// Tính điểm IOI cho một submission. Mỗi test case AC = 1 điểm.
    /// </summary>
    private async Task<IoiScoreResult> CalcIoiScoreAsync(Guid submissionId, CancellationToken ct)
    {
        // Lấy tất cả Result của submission có gắn TestcaseId (loại bỏ row compile)
        // Không filter theo Type để tránh mất data do label khác nhau ("judge"/"run"/null...)
        var results = await _db.Results
            .AsNoTracking()
            .Where(r => r.SubmissionId == submissionId && r.TestcaseId != null)
            .Select(r => new
            {
                r.TestcaseId,
                r.StatusCode,
                r.RuntimeMs,
                r.MemoryKb,
                r.Type,
                Ordinal = (int?)_db.Testcases
                    .Where(t => t.Id == r.TestcaseId)
                    .Select(t => (int?)t.Ordinal)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        if (results.Count == 0)
            return new IoiScoreResult(0, 0, 0, []);

        var ordered = results.OrderBy(r => r.Ordinal ?? int.MaxValue).ToList();
        int passedCases = ordered.Count(r => r.StatusCode == "ac");

        var caseDetails = ordered.Select(r => new
        {
            r.TestcaseId,
            r.Ordinal,
            verdict = r.StatusCode,
            passed = r.StatusCode == "ac",
            r.RuntimeMs,
            r.MemoryKb,
            r.Type
        }).ToList<object>();

        return new IoiScoreResult(
            TotalScore: passedCases,
            PassedCases: passedCases,
            TotalCases: ordered.Count,
            Cases: caseDetails);
    }

    private sealed record IoiScoreResult(
        int TotalScore,
        int PassedCases,
        int TotalCases,
        List<object> Cases);

    private async Task<AcmProblemResult> CalcAcmProblemAsync(
        Domain.Entities.ContestProblem cp,
        DateTime contestStartAt,
        Guid teamId,
        CancellationToken ct)
    {
        const int penaltyPerWrong = 20; // chuẩn ACM: mỗi lần sai = +20 phút penalty

        var submissions = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.ContestProblemId == cp.Id
                        && s.TeamId == teamId
                        && s.StatusCode == "done")
            .OrderBy(s => s.CreatedAt)
            .Select(s => new { s.Id, s.VerdictCode, s.CreatedAt })
            .ToListAsync(ct);

        var firstAc = submissions.FirstOrDefault(s => s.VerdictCode == "ac");

        var beforeAc = firstAc is null
            ? submissions
            : submissions.Where(s => s.CreatedAt < firstAc.CreatedAt).ToList();

        int wrongAttempts = beforeAc.Count(s =>
            s.VerdictCode != null && WrongVerdicts.Contains(s.VerdictCode));

        int penaltyMinutes = 0;
        if (firstAc is not null)
        {
            var minutesFromStart = (int)Math.Floor((firstAc.CreatedAt - contestStartAt).TotalMinutes);
            penaltyMinutes = minutesFromStart + wrongAttempts * penaltyPerWrong;
        }

        return new AcmProblemResult(
            Solved: firstAc is not null,
            WrongAttempts: wrongAttempts,
            PenaltyMinutes: penaltyMinutes,
            FirstAcAt: firstAc?.CreatedAt,
            TotalSubmissions: submissions.Count,
            SubmissionHistory: submissions.Select(s => new
            {
                s.Id,
                verdict = s.VerdictCode,
                submittedAt = s.CreatedAt
            }).ToList<object>()
        );
    }

    private sealed record AcmProblemResult(
        bool Solved,
        int WrongAttempts,
        int PenaltyMinutes,
        DateTime? FirstAcAt,
        int TotalSubmissions,
        List<object> SubmissionHistory);
}
