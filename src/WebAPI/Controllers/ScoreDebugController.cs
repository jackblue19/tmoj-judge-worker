using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,manager,teacher")]
    public class ScoreDebugController : ControllerBase
    {
        private readonly TmojDbContext _db;

        public ScoreDebugController(TmojDbContext db)
        {
            _db = db;
        }

        // ──────────────────────────────────────────
        // POST api/ScoreDebug/calculate/acm  →  Calculate ACM/ICPC score for a submission
        // ──────────────────────────────────────────
        /// <summary>
        /// ACM/ICPC scoring: Binary verdict.
        /// All testcases must pass (StatusCode == "ac") → FinalScore = 100, VerdictCode = "ac".
        /// Any failure → FinalScore = 0, VerdictCode = worst status found.
        /// </summary>
        [HttpPost("calculate/acm")]
        public async Task<IActionResult> CalculateAcmScore(
            [FromBody] CalculateScoreRequest req,
            CancellationToken ct)
        {
            try
            {
                var submission = await _db.Submissions
                    .Include(s => s.Results)
                    .FirstOrDefaultAsync(s => s.Id == req.SubmissionId, ct);

                if (submission is null)
                    return NotFound(new { Message = "Submission not found." });

                var results = submission.Results.ToList();
                if (results.Count == 0)
                    return BadRequest(new { Message = "Submission has no results to score." });

                var allAccepted = results.All(r => r.StatusCode == "ac");

                var verdictCode = allAccepted
                    ? "ac"
                    : DetermineWorstVerdict(results.Select(r => r.StatusCode).ToList());

                var finalScore = allAccepted ? 100m : 0m;

                var maxTimeMs = results.Where(r => r.RuntimeMs.HasValue).Select(r => r.RuntimeMs!.Value).DefaultIfEmpty(0).Max();
                var maxMemoryKb = results.Where(r => r.MemoryKb.HasValue).Select(r => r.MemoryKb!.Value).DefaultIfEmpty(0).Max();

                if (req.ApplyToSubmission)
                {
                    submission.FinalScore = finalScore;
                    submission.VerdictCode = verdictCode;
                    submission.TimeMs = maxTimeMs;
                    submission.MemoryKb = maxMemoryKb;
                    submission.JudgedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                }

                return Ok(ApiResponse<AcmScoreResult>.Ok(new AcmScoreResult(
                    submission.Id,
                    "acm",
                    verdictCode,
                    finalScore,
                    results.Count,
                    results.Count(r => r.StatusCode == "ac"),
                    maxTimeMs,
                    maxMemoryKb,
                    req.ApplyToSubmission
                ), "ACM score calculated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while calculating ACM score.", Detail = ex.Message });
            }
        }

        // ──────────────────────────────────────────
        // POST api/ScoreDebug/calculate/ioi  →  Calculate IOI score for a submission
        // ──────────────────────────────────────────
        /// <summary>
        /// IOI scoring: Partial score based on testcase weights.
        /// Score = (sum of weights of passed testcases / sum of all weights) × 100.
        /// Each result is matched with its testcase's Weight; if no testcase is linked, weight defaults to 1.
        /// </summary>
        [HttpPost("calculate/ioi")]
        public async Task<IActionResult> CalculateIoiScore(
            [FromBody] CalculateScoreRequest req,
            CancellationToken ct)
        {
            try
            {
                var submission = await _db.Submissions
                    .Include(s => s.Results).ThenInclude(r => r.Testcase)
                    .FirstOrDefaultAsync(s => s.Id == req.SubmissionId, ct);

                if (submission is null)
                    return NotFound(new { Message = "Submission not found." });

                var results = submission.Results.ToList();
                if (results.Count == 0)
                    return BadRequest(new { Message = "Submission has no results to score." });

                int totalWeight = 0;
                int passedWeight = 0;
                var testcaseDetails = new List<IoiTestcaseScore>();

                foreach (var r in results.OrderBy(r => r.Testcase?.Ordinal ?? 0))
                {
                    int weight = r.Testcase?.Weight ?? 1;
                    bool passed = r.StatusCode == "ac";
                    totalWeight += weight;
                    if (passed) passedWeight += weight;

                    testcaseDetails.Add(new IoiTestcaseScore(
                        r.TestcaseId,
                        r.Testcase?.Ordinal ?? 0,
                        r.StatusCode,
                        weight,
                        passed));
                }

                decimal finalScore = totalWeight > 0
                    ? Math.Round((decimal)passedWeight / totalWeight * 100, 2)
                    : 0m;

                var verdictCode = finalScore == 100m
                    ? "ac"
                    : (finalScore > 0 ? "partial" : DetermineWorstVerdict(results.Select(r => r.StatusCode).ToList()));

                var maxTimeMs = results.Where(r => r.RuntimeMs.HasValue).Select(r => r.RuntimeMs!.Value).DefaultIfEmpty(0).Max();
                var maxMemoryKb = results.Where(r => r.MemoryKb.HasValue).Select(r => r.MemoryKb!.Value).DefaultIfEmpty(0).Max();

                if (req.ApplyToSubmission)
                {
                    submission.FinalScore = finalScore;
                    submission.VerdictCode = verdictCode;
                    submission.TimeMs = maxTimeMs;
                    submission.MemoryKb = maxMemoryKb;
                    submission.JudgedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                }

                return Ok(ApiResponse<IoiScoreResult>.Ok(new IoiScoreResult(
                    submission.Id,
                    "ioi",
                    verdictCode,
                    finalScore,
                    results.Count,
                    results.Count(r => r.StatusCode == "ac"),
                    totalWeight,
                    passedWeight,
                    maxTimeMs,
                    maxMemoryKb,
                    req.ApplyToSubmission,
                    testcaseDetails
                ), "IOI score calculated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while calculating IOI score.", Detail = ex.Message });
            }
        }

        // ──────────────────────────────────────────
        // POST api/ScoreDebug/contest/acm  →  Recalculate ACM scoreboard for a contest
        // ──────────────────────────────────────────
        /// <summary>
        /// ACM/ICPC contest scoring:
        /// For each team × problem: solved = all testcases AC, penalty = minutes to first AC + 20 × wrong attempts.
        /// Updates ContestScoreboard, ContestScoreboardEntry, and ContestTeam.
        /// </summary>
        [HttpPost("contest/acm")]
        public async Task<IActionResult> CalculateContestAcmScore(
            [FromBody] ContestScoreRequest req,
            CancellationToken ct)
        {
            try
            {
                var contest = await _db.Contests
                    .Include(c => c.ContestProblems)
                    .Include(c => c.ContestTeams)
                    .FirstOrDefaultAsync(c => c.Id == req.ContestId, ct);

                if (contest is null)
                    return NotFound(new { Message = "Contest not found." });

                var contestProblems = contest.ContestProblems.ToList();
                var contestTeams = contest.ContestTeams.ToList();

                if (contestProblems.Count == 0)
                    return BadRequest(new { Message = "Contest has no problems." });
                if (contestTeams.Count == 0)
                    return BadRequest(new { Message = "Contest has no participants." });

                // Load all submissions for this contest's problems
                var contestProblemIds = contestProblems.Select(cp => cp.Id).ToList();
                var allSubmissions = await _db.Submissions.AsNoTracking()
                    .Include(s => s.Results)
                    .Where(s => s.ContestProblemId != null && contestProblemIds.Contains(s.ContestProblemId.Value))
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync(ct);

                var teamResults = new List<ContestTeamAcmResult>();

                foreach (var team in contestTeams)
                {
                    // Get team member user IDs
                    var teamMemberIds = await _db.TeamMembers.AsNoTracking()
                        .Where(tm => tm.TeamId == team.TeamId)
                        .Select(tm => tm.UserId)
                        .ToListAsync(ct);

                    int totalSolved = 0;
                    int totalPenalty = 0;
                    var problemResults = new List<ContestProblemAcmResult>();

                    foreach (var cp in contestProblems)
                    {
                        var problemSubs = allSubmissions
                            .Where(s => s.ContestProblemId == cp.Id && teamMemberIds.Contains(s.UserId))
                            .OrderBy(s => s.CreatedAt)
                            .ToList();

                        bool solved = false;
                        int attempts = 0;
                        int penaltyMinutes = 0;
                        DateTime? firstAcAt = null;

                        foreach (var sub in problemSubs)
                        {
                            var subResults = sub.Results.ToList();
                            var allAc = subResults.Count > 0 && subResults.All(r => r.StatusCode == "ac");

                            if (allAc && !solved)
                            {
                                solved = true;
                                firstAcAt = sub.CreatedAt;
                                int minutesToSolve = (int)(sub.CreatedAt - contest.StartAt).TotalMinutes;
                                penaltyMinutes = minutesToSolve + (attempts * 20);
                                break;
                            }
                            attempts++;
                        }

                        if (solved) totalSolved++;
                        totalPenalty += penaltyMinutes;

                        // Find best & last submission
                        var bestSub = problemSubs.FirstOrDefault(s => s.Results.All(r => r.StatusCode == "ac") && s.Results.Count > 0);
                        var lastSub = problemSubs.LastOrDefault();

                        problemResults.Add(new ContestProblemAcmResult(
                            cp.ProblemId, solved, attempts + (solved ? 1 : 0), penaltyMinutes, firstAcAt));

                        if (req.Apply)
                        {
                            // Upsert ContestScoreboard
                            var sb = await _db.ContestScoreboards
                                .FirstOrDefaultAsync(s => s.ContestId == contest.Id && s.EntryId == team.Id && s.ProblemId == cp.ProblemId, ct);

                            if (sb is null)
                            {
                                sb = new Domain.Entities.ContestScoreboard
                                {
                                    ContestId = contest.Id,
                                    EntryId = team.Id,
                                    ProblemId = cp.ProblemId
                                };
                                _db.ContestScoreboards.Add(sb);
                            }
                            sb.AcmSolved = solved;
                            sb.AcmAttempts = attempts + (solved ? 1 : 0);
                            sb.AcmPenaltyTime = penaltyMinutes;
                            sb.FirstAcAt = firstAcAt;
                            sb.BestScore = solved ? 100m : 0m;
                            sb.LastScore = lastSub?.FinalScore ?? 0m;
                            sb.BestSubmissionId = bestSub?.Id;
                            sb.LastSubmissionId = lastSub?.Id;
                            sb.LastSubmitAt = lastSub?.CreatedAt;
                        }
                    }

                    teamResults.Add(new ContestTeamAcmResult(
                        team.TeamId, totalSolved, totalPenalty, problemResults));

                    if (req.Apply)
                    {
                        // Upsert ContestScoreboardEntry
                        var entry = await _db.ContestScoreboardEntries
                            .FirstOrDefaultAsync(e => e.ContestId == contest.Id && e.EntryId == team.Id, ct);

                        if (entry is null)
                        {
                            entry = new Domain.Entities.ContestScoreboardEntry
                            {
                                ContestId = contest.Id,
                                EntryId = team.Id
                            };
                            _db.ContestScoreboardEntries.Add(entry);
                        }
                        entry.TotalScore = totalSolved;
                        entry.SolvedCount = totalSolved;
                        entry.PenaltyTime = totalPenalty;
                        entry.LastSolveAt = problemResults
                            .Where(p => p.FirstAcAt.HasValue)
                            .Select(p => p.FirstAcAt!.Value)
                            .DefaultIfEmpty(DateTime.MinValue).Max();
                        entry.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Rank teams: more solved first, then less penalty
                var ranked = teamResults
                    .OrderByDescending(t => t.SolvedCount)
                    .ThenBy(t => t.TotalPenalty)
                    .ToList();

                if (req.Apply)
                {
                    int rank = 1;
                    foreach (var t in ranked)
                    {
                        var team = contestTeams.First(ct2 => ct2.TeamId == t.TeamId);
                        team.Score = t.SolvedCount;
                        team.SolvedProblem = t.SolvedCount;
                        team.Penalty = t.TotalPenalty;
                        team.Rank = rank;

                        var entry = await _db.ContestScoreboardEntries
                            .FirstOrDefaultAsync(e => e.ContestId == contest.Id && e.EntryId == team.Id, ct);
                        if (entry != null) entry.Rank = rank;

                        rank++;
                    }
                    await _db.SaveChangesAsync(ct);
                }

                return Ok(ApiResponse<ContestAcmScoreResult>.Ok(new ContestAcmScoreResult(
                    contest.Id, "acm", contestProblems.Count, contestTeams.Count,
                    req.Apply, ranked
                ), "Contest ACM scoreboard calculated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while calculating contest ACM score.", Detail = ex.Message });
            }
        }

        // ──────────────────────────────────────────
        // POST api/ScoreDebug/contest/ioi  →  Recalculate IOI scoreboard for a contest
        // ──────────────────────────────────────────
        /// <summary>
        /// IOI contest scoring:
        /// For each team × problem: best score across all submissions (partial scoring with testcase weights).
        /// Points = (passedWeight / totalWeight) × maxPoints of the contest problem.
        /// Updates ContestScoreboard, ContestScoreboardEntry, and ContestTeam.
        /// </summary>
        [HttpPost("contest/ioi")]
        public async Task<IActionResult> CalculateContestIoiScore(
            [FromBody] ContestScoreRequest req,
            CancellationToken ct)
        {
            try
            {
                var contest = await _db.Contests
                    .Include(c => c.ContestProblems)
                    .Include(c => c.ContestTeams)
                    .FirstOrDefaultAsync(c => c.Id == req.ContestId, ct);

                if (contest is null)
                    return NotFound(new { Message = "Contest not found." });

                var contestProblems = contest.ContestProblems.ToList();
                var contestTeams = contest.ContestTeams.ToList();

                if (contestProblems.Count == 0)
                    return BadRequest(new { Message = "Contest has no problems." });
                if (contestTeams.Count == 0)
                    return BadRequest(new { Message = "Contest has no participants." });

                // Load all submissions with results + testcases
                var contestProblemIds = contestProblems.Select(cp => cp.Id).ToList();
                var allSubmissions = await _db.Submissions.AsNoTracking()
                    .Include(s => s.Results).ThenInclude(r => r.Testcase)
                    .Where(s => s.ContestProblemId != null && contestProblemIds.Contains(s.ContestProblemId.Value))
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync(ct);

                var teamResults = new List<ContestTeamIoiResult>();

                foreach (var team in contestTeams)
                {
                    var teamMemberIds = await _db.TeamMembers.AsNoTracking()
                        .Where(tm => tm.TeamId == team.TeamId)
                        .Select(tm => tm.UserId)
                        .ToListAsync(ct);

                    decimal totalScore = 0;
                    int solvedCount = 0;
                    var problemResults = new List<ContestProblemIoiResult>();

                    foreach (var cp in contestProblems)
                    {
                        int maxPoints = cp.Points ?? cp.MaxScore ?? 100;

                        var problemSubs = allSubmissions
                            .Where(s => s.ContestProblemId == cp.Id && teamMemberIds.Contains(s.UserId))
                            .ToList();

                        decimal bestScore = 0m;
                        Guid? bestSubId = null;
                        Guid? lastSubId = null;
                        DateTime? lastSubmitAt = null;

                        foreach (var sub in problemSubs)
                        {
                            var subResults = sub.Results.ToList();
                            if (subResults.Count == 0) continue;

                            int tw = 0, pw = 0;
                            foreach (var r in subResults)
                            {
                                int w = r.Testcase?.Weight ?? 1;
                                tw += w;
                                if (r.StatusCode == "ac") pw += w;
                            }

                            decimal subScore = tw > 0
                                ? Math.Round((decimal)pw / tw * maxPoints, 2)
                                : 0m;

                            if (subScore > bestScore)
                            {
                                bestScore = subScore;
                                bestSubId = sub.Id;
                            }

                            lastSubId = sub.Id;
                            lastSubmitAt = sub.CreatedAt;
                        }

                        bool fullySolved = bestScore >= maxPoints;
                        if (fullySolved) solvedCount++;
                        totalScore += bestScore;

                        problemResults.Add(new ContestProblemIoiResult(
                            cp.ProblemId, bestScore, maxPoints, problemSubs.Count, fullySolved));

                        if (req.Apply)
                        {
                            var sb = await _db.ContestScoreboards
                                .FirstOrDefaultAsync(s => s.ContestId == contest.Id && s.EntryId == team.Id && s.ProblemId == cp.ProblemId, ct);

                            if (sb is null)
                            {
                                sb = new Domain.Entities.ContestScoreboard
                                {
                                    ContestId = contest.Id,
                                    EntryId = team.Id,
                                    ProblemId = cp.ProblemId
                                };
                                _db.ContestScoreboards.Add(sb);
                            }
                            sb.AcmSolved = fullySolved;
                            sb.AcmAttempts = problemSubs.Count;
                            sb.AcmPenaltyTime = 0;
                            sb.BestScore = bestScore;
                            sb.LastScore = problemSubs.LastOrDefault()?.FinalScore ?? 0m;
                            sb.BestSubmissionId = bestSubId;
                            sb.LastSubmissionId = lastSubId;
                            sb.LastSubmitAt = lastSubmitAt;
                        }
                    }

                    totalScore = Math.Round(totalScore, 2);
                    teamResults.Add(new ContestTeamIoiResult(
                        team.TeamId, totalScore, solvedCount, problemResults));

                    if (req.Apply)
                    {
                        var entry = await _db.ContestScoreboardEntries
                            .FirstOrDefaultAsync(e => e.ContestId == contest.Id && e.EntryId == team.Id, ct);

                        if (entry is null)
                        {
                            entry = new Domain.Entities.ContestScoreboardEntry
                            {
                                ContestId = contest.Id,
                                EntryId = team.Id
                            };
                            _db.ContestScoreboardEntries.Add(entry);
                        }
                        entry.TotalScore = totalScore;
                        entry.SolvedCount = solvedCount;
                        entry.PenaltyTime = 0;
                        entry.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Rank teams: highest total score first, then more problems solved
                var ranked = teamResults
                    .OrderByDescending(t => t.TotalScore)
                    .ThenByDescending(t => t.SolvedCount)
                    .ToList();

                if (req.Apply)
                {
                    int rank = 1;
                    foreach (var t in ranked)
                    {
                        var team = contestTeams.First(ct2 => ct2.TeamId == t.TeamId);
                        team.Score = t.TotalScore;
                        team.SolvedProblem = t.SolvedCount;
                        team.Penalty = 0;
                        team.Rank = rank;

                        var entry = await _db.ContestScoreboardEntries
                            .FirstOrDefaultAsync(e => e.ContestId == contest.Id && e.EntryId == team.Id, ct);
                        if (entry != null) entry.Rank = rank;

                        rank++;
                    }
                    await _db.SaveChangesAsync(ct);
                }

                return Ok(ApiResponse<ContestIoiScoreResult>.Ok(new ContestIoiScoreResult(
                    contest.Id, "ioi", contestProblems.Count, contestTeams.Count,
                    req.Apply, ranked
                ), "Contest IOI scoreboard calculated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while calculating contest IOI score.", Detail = ex.Message });
            }
        }

        // ── Helpers ───────────────────────────────

        /// <summary>
        /// Determines the "worst" verdict from a list of status codes.
        /// Priority: ce > re > mle > tle > wa > other
        /// </summary>
        private static string DetermineWorstVerdict(List<string?> statusCodes)
        {
            var priorities = new[] { "ce", "re", "mle", "tle", "wa" };
            foreach (var p in priorities)
            {
                if (statusCodes.Any(s => s == p))
                    return p;
            }
            return statusCodes.FirstOrDefault(s => s != "ac") ?? "wa";
        }
    }

    // ── Request / Response DTOs ───────────────────

    // Submission-level
    public record CalculateScoreRequest(
        Guid SubmissionId,
        bool ApplyToSubmission = false);

    public record AcmScoreResult(
        Guid SubmissionId,
        string ScoringType,
        string VerdictCode,
        decimal FinalScore,
        int TotalTestcases,
        int PassedTestcases,
        int MaxTimeMs,
        int MaxMemoryKb,
        bool Applied);

    public record IoiScoreResult(
        Guid SubmissionId,
        string ScoringType,
        string VerdictCode,
        decimal FinalScore,
        int TotalTestcases,
        int PassedTestcases,
        int TotalWeight,
        int PassedWeight,
        int MaxTimeMs,
        int MaxMemoryKb,
        bool Applied,
        List<IoiTestcaseScore> TestcaseDetails);

    public record IoiTestcaseScore(
        Guid? TestcaseId,
        int Ordinal,
        string? StatusCode,
        int Weight,
        bool Passed);

    // Contest-level
    public record ContestScoreRequest(
        Guid ContestId,
        bool Apply = false);

    // ACM contest
    public record ContestAcmScoreResult(
        Guid ContestId,
        string ScoringType,
        int ProblemCount,
        int TeamCount,
        bool Applied,
        List<ContestTeamAcmResult> Rankings);

    public record ContestTeamAcmResult(
        Guid TeamId,
        int SolvedCount,
        int TotalPenalty,
        List<ContestProblemAcmResult> Problems);

    public record ContestProblemAcmResult(
        Guid ProblemId,
        bool Solved,
        int Attempts,
        int PenaltyMinutes,
        DateTime? FirstAcAt);

    // IOI contest
    public record ContestIoiScoreResult(
        Guid ContestId,
        string ScoringType,
        int ProblemCount,
        int TeamCount,
        bool Applied,
        List<ContestTeamIoiResult> Rankings);

    public record ContestTeamIoiResult(
        Guid TeamId,
        decimal TotalScore,
        int SolvedCount,
        List<ContestProblemIoiResult> Problems);

    public record ContestProblemIoiResult(
        Guid ProblemId,
        decimal BestScore,
        int MaxPoints,
        int Attempts,
        bool FullySolved);
}
