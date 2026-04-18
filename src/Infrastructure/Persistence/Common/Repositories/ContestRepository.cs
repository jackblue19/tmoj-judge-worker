using Application.Common.Interfaces;
using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Teams.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class ContestRepository : IContestRepository
{
    private readonly TmojDbContext _db;

    public ContestRepository(TmojDbContext db)
    {
        _db = db;
    }

    // =============================================
    // GET CONTEST LIST
    // =============================================
    public async Task<PagedResult<ContestDto>> GetContestsAsync(
        string? status,
        string? visibilityCode,
        bool includeArchived,
        int page,
        int pageSize)
    {
        var now = DateTime.UtcNow;

        var visibility = string.IsNullOrEmpty(visibilityCode)
            ? "public"
            : visibilityCode.ToLower();

        var query = _db.Contests
            .AsNoTracking()
            .Where(x => x.VisibilityCode == visibility);

        if (!includeArchived)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrEmpty(status))
        {
            status = status.ToLower();

            if (status == "upcoming")
                query = query.Where(x => x.StartAt > now);
            else if (status == "running")
                query = query.Where(x => x.StartAt <= now && x.EndAt >= now);
            else if (status == "ended")
                query = query.Where(x => x.EndAt < now);
        }

        var all = await query.ToListAsync();

        var sorted = all
            .Select(x => new
            {
                Contest = x,
                Priority =
                    x.StartAt <= now && x.EndAt >= now ? 0 :
                    x.StartAt > now ? 1 :
                    2
            })
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Contest.StartAt)
            .Select(x => x.Contest)
            .ToList();

        var total = sorted.Count;

        var paged = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = paged.Select(x => new ContestDto
        {
            Id = x.Id,
            Title = x.Title,
            StartAt = x.StartAt,
            EndAt = x.EndAt,
            VisibilityCode = x.VisibilityCode,
            ContestType = x.ContestType,
            AllowTeams = x.AllowTeams,
            Status =
                x.StartAt > now ? "upcoming" :
                x.EndAt < now ? "ended" :
                "running"
        }).ToList();

        return new PagedResult<ContestDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // =============================================
    // GET DETAIL
    // =============================================
    public async Task<ContestDetailDto?> GetContestDetailAsync(Guid contestId)
    {
        var now = DateTime.UtcNow;

        var contest = await _db.Contests
            .AsNoTracking()
            .Include(c => c.ContestProblems!)
                .ThenInclude(cp => cp.Problem)
            .FirstOrDefaultAsync(c => c.Id == contestId);

        if (contest == null) return null;

        // =========================
        // ❌ STRICT FREEZE - BLOCK VIEW DETAIL
        // =========================
        if (contest.FreezeAt.HasValue && now >= contest.FreezeAt.Value)
            throw new Exception("CONTEST_FROZEN_VIEW_BLOCKED");

        var problems = contest.ContestProblems!
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayIndex ?? p.Ordinal ?? 999)
            .ThenBy(p => p.Alias)
            .ToList();

        var freezeAt = contest.FreezeAt;

        var startAt = contest.StartAt;
        var endAt = contest.EndAt;

        var durationMinutes =
            endAt != default && startAt != default
                ? (int)(endAt - startAt).TotalMinutes
                : 0;

        return new ContestDetailDto
        {
            Id = contest.Id,
            Title = contest.Title,
            Description = contest.DescriptionMd ?? "",
            Slug = contest.Slug ?? "",
            Visibility = contest.VisibilityCode,
            ContestType = contest.ContestType ?? "icpc",
            AllowTeams = contest.AllowTeams,

            Status =
                startAt > now ? "upcoming" :
                endAt < now ? "ended" :
                "running",

            IsPublished = contest.VisibilityCode == "public",

            // freeze info (optional for UI)
            IsFrozen = freezeAt.HasValue && now >= freezeAt.Value,
            FreezeAt = freezeAt,

            CanJoin = !(freezeAt.HasValue && now >= freezeAt.Value) && now < startAt,
            HasLeaderboard = now >= startAt,

            StartAt = startAt,
            EndAt = endAt,
            DurationMinutes = durationMinutes,

            ProblemCount = problems.Count,
            TotalPoints = problems.Sum(p => p.Points ?? 0),

            Problems = problems.Select(p => new ContestProblemDto
            {
                Id = p.Id,
                ProblemId = p.ProblemId,
                Title = p.Problem.Title,
                Alias = p.Alias,
                Ordinal = p.Ordinal,
                DisplayIndex = p.DisplayIndex,
                Points = p.Points ?? 0,
                TimeLimitMs = p.TimeLimitMs,
                MemoryLimitKb = p.MemoryLimitKb
            }).ToList()
        };
    }
    // =============================================
    // CHECK JOIN
    // =============================================
    public async Task<bool> IsTeamJoinedAsync(Guid contestId, Guid teamId)
    {
        return await _db.ContestTeams
            .AnyAsync(x => x.ContestId == contestId && x.TeamId == teamId);
    }

    public async Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId)
    {
        return await _db.TeamMembers
            .AnyAsync(x => x.TeamId == teamId && x.UserId == userId);
    }

    // =============================================
    // TEAM MEMBERS
    // =============================================
    public async Task<List<Guid>> GetTeamMemberIdsAsync(Guid teamId)
    {
        return await _db.TeamMembers
            .Where(x => x.TeamId == teamId)
            .Select(x => x.UserId)
            .ToListAsync();
    }

    // =============================================
    // TIME CONFLICT
    // =============================================
    public async Task<bool> HasTimeConflictAsync(Guid userId, DateTime start, DateTime end)
    {
        return await _db.ContestTeams
            .Where(ct => ct.Team.TeamMembers.Any(m => m.UserId == userId))
            .AnyAsync(ct =>
                ct.Contest.StartAt < end &&
                ct.Contest.EndAt > start
            );
    }

    // =============================================
    // ACTIVE CONTEST
    // =============================================
    public async Task<Contest?> GetActiveContestByTeamIdAsync(Guid teamId)
    {
        return await _db.ContestTeams
            .Where(ct => ct.TeamId == teamId)
            .Select(ct => ct.Contest)
            .OrderByDescending(c => c.StartAt)
            .FirstOrDefaultAsync();
    }

    // =============================================
    // CONTEST TEAM
    // =============================================
    public async Task<ContestTeam?> GetContestTeamAsync(Guid contestId, Guid teamId)
    {
        return await _db.ContestTeams
            .FirstOrDefaultAsync(x =>
                x.ContestId == contestId &&
                x.TeamId == teamId);
    }

    // =============================================
    // GET MY TEAM IN CONTEST
    // =============================================
    public async Task<MyTeamInContestDto?> GetMyTeamInContestAsync(Guid contestId, Guid userId)
    {
        return await _db.ContestTeams
            .Where(ct => ct.ContestId == contestId)
            .Where(ct => ct.Team.TeamMembers.Any(m => m.UserId == userId))
            .Select(ct => new MyTeamInContestDto
            {
                ContestId = ct.ContestId,
                TeamId = ct.Team.Id,
                TeamName = ct.Team.TeamName,
                LeaderId = ct.Team.LeaderId,
                TeamSize = ct.Team.TeamSize,
                MemberCount = ct.Team.TeamMembers.Count(),
                JoinedAt = ct.JoinAt,
                Rank = ct.Rank,
                Score = ct.Score,
                Members = ct.Team.TeamMembers.Select(m => new TeamMemberDto
                {
                    UserId = m.UserId,
                    UserName = m.User.Username,
                    JoinedAt = m.JoinedAt
                }).ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    // =============================================
    // SCOREBOARD (FREEZE FIXED) — branch ACM vs IOI theo Contest.ContestType
    // =============================================
    public async Task<List<ContestScoreboardDto>> GetScoreboardAsync(Guid contestId)
    {
        var contest = await _db.Contests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contestId);

        if (contest == null)
            return new List<ContestScoreboardDto>();

        var freezeTime = contest.FreezeAt;
        var now = DateTime.UtcNow;
        var isFrozen = freezeTime.HasValue && now >= freezeTime.Value;
        var isAcm = ScoringHelper.IsAcmContest(contest);

        var teams = await _db.ContestTeams
            .AsNoTracking()
            .Where(ct => ct.ContestId == contestId)
            .Select(ct => new ScoreboardTeamRow(ct.TeamId, ct.Team.TeamName))
            .ToListAsync();

        var problems = await _db.ContestProblems
            .AsNoTracking()
            .Where(p => p.ContestId == contestId && p.IsActive)
            .Select(p => new ScoreboardProblemRow(p.Id, p.ProblemId, p.Alias))
            .ToListAsync();

        var contestProblemIds = problems.Select(p => p.ContestProblemId).ToList();

        var submissions = await _db.Submissions
            .AsNoTracking()
            .Where(s =>
                s.ContestProblemId != null &&
                contestProblemIds.Contains(s.ContestProblemId.Value) &&
                s.TeamId != null)
            .Select(s => new ScoreboardSubmissionRow(
                s.Id,
                s.TeamId,
                s.ContestProblemId,
                s.VerdictCode,
                s.CreatedAt))
            .ToListAsync();

        if (isFrozen && freezeTime.HasValue)
            submissions = submissions.Where(s => s.CreatedAt <= freezeTime.Value).ToList();

        // IOI cần Result + Testcase để cộng điểm theo Weight. ACM thì không cần.
        Dictionary<Guid, List<Result>>? resultsBySubmission = null;
        Dictionary<Guid, (int Ordinal, int Weight)>? testcaseInfo = null;

        if (!isAcm)
        {
            var submissionIds = submissions.Select(s => s.Id).Distinct().ToList();

            resultsBySubmission = submissionIds.Count == 0
                ? new Dictionary<Guid, List<Result>>()
                : (await _db.Results
                    .AsNoTracking()
                    .Where(r => submissionIds.Contains(r.SubmissionId) && r.TestcaseId != null)
                    .ToListAsync())
                    .GroupBy(r => r.SubmissionId)
                    .ToDictionary(g => g.Key, g => g.ToList());

            var testcaseIds = resultsBySubmission.Values
                .SelectMany(rs => rs)
                .Where(r => r.TestcaseId.HasValue)
                .Select(r => r.TestcaseId!.Value)
                .Distinct()
                .ToList();

            testcaseInfo = testcaseIds.Count == 0
                ? new Dictionary<Guid, (int Ordinal, int Weight)>()
                : (await _db.Testcases
                    .AsNoTracking()
                    .Where(t => testcaseIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Ordinal, t.Weight })
                    .ToListAsync())
                    .ToDictionary(t => t.Id, t => (t.Ordinal, t.Weight));
        }

        var result = new List<ContestScoreboardDto>();

        foreach (var team in teams)
        {
            var teamSubs = submissions.Where(s => s.TeamId == team.TeamId).ToList();

            int solved = 0;
            int penalty = 0;
            int totalScore = 0;
            var problemStats = new List<ScoreboardProblemDto>();

            foreach (var problem in problems)
            {
                var subs = teamSubs
                    .Where(s => s.ContestProblemId == problem.ContestProblemId)
                    .OrderBy(s => s.CreatedAt)
                    .ToList();

                if (isAcm)
                {
                    int wrong = 0;
                    DateTime? acTime = null;

                    foreach (var sub in subs)
                    {
                        bool isAc = !string.IsNullOrEmpty(sub.VerdictCode)
                            && sub.VerdictCode.Equals("ac", StringComparison.OrdinalIgnoreCase);

                        if (acTime == null && isAc)
                        {
                            acTime = sub.CreatedAt;
                            solved++;
                            var minutes = (int)(acTime.Value - contest.StartAt).TotalMinutes;
                            penalty += minutes + wrong * ScoringHelper.AcmPenaltyPerWrong;
                        }
                        else if (!isAc)
                        {
                            wrong++;
                        }
                    }

                    problemStats.Add(new ScoreboardProblemDto
                    {
                        ProblemId = problem.ProblemId,
                        Alias = problem.Alias ?? "",
                        IsSolved = acTime != null,
                        Attempts = subs.Count,
                        SolvedAt = acTime
                    });
                }
                else
                {
                    int bestScore = 0;
                    int bestPassed = 0;
                    int bestTotal = 0;
                    DateTime? firstFullAc = null;

                    foreach (var s in subs)
                    {
                        if (!resultsBySubmission!.TryGetValue(s.Id, out var results) || results.Count == 0)
                            continue;

                        var (score, passed, total, _) =
                            ScoringHelper.CalcIoiScore(results, testcaseInfo!);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestPassed = passed;
                            bestTotal = total;
                        }

                        if (firstFullAc == null && total > 0 && passed == total)
                            firstFullAc = s.CreatedAt;
                    }

                    if (firstFullAc != null)
                        solved++;

                    totalScore += bestScore;

                    problemStats.Add(new ScoreboardProblemDto
                    {
                        ProblemId = problem.ProblemId,
                        Alias = problem.Alias ?? "",
                        IsSolved = firstFullAc != null,
                        Attempts = subs.Count,
                        SolvedAt = firstFullAc,
                        Score = bestScore,
                        PassedCases = bestPassed,
                        TotalCases = bestTotal
                    });
                }
            }

            result.Add(new ContestScoreboardDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                ScoringMode = isAcm ? "acm" : "ioi",
                Solved = solved,
                Penalty = isAcm ? penalty : 0,
                TotalScore = isAcm ? 0 : totalScore,
                Problems = problemStats
            });
        }

        var sorted = isAcm
            ? result.OrderByDescending(x => x.Solved).ThenBy(x => x.Penalty).ToList()
            : result.OrderByDescending(x => x.TotalScore).ToList();

        for (int i = 0; i < sorted.Count; i++)
            sorted[i].Rank = i + 1;

        return sorted;
    }

    private sealed record ScoreboardTeamRow(Guid TeamId, string TeamName);
    private sealed record ScoreboardProblemRow(Guid ContestProblemId, Guid ProblemId, string? Alias);
    private sealed record ScoreboardSubmissionRow(
        Guid Id,
        Guid? TeamId,
        Guid? ContestProblemId,
        string? VerdictCode,
        DateTime CreatedAt);

    // =============================================
    // GET MY CONTESTS
    // =============================================
    public async Task<List<Contest>> GetMyContestsAsync(Guid userId)
    {
        return await _db.ContestTeams
            .Where(ct => ct.Team.TeamMembers.Any(tm => tm.UserId == userId))
            .Select(ct => ct.Contest)
            .Distinct()
            .ToListAsync();
    }

    // =============================================
    // GET MY CONTESTS DETAILED
    // =============================================
    public async Task<List<MyContestDto>> GetMyContestsDetailedAsync(Guid userId)
    {
        return await _db.ContestTeams
            .Where(ct => ct.Team.TeamMembers.Any(tm => tm.UserId == userId))
            .Select(ct => new MyContestDto
            {
                ContestId = ct.Contest.Id,
                Title = ct.Contest.Title,
                StartAt = ct.Contest.StartAt,
                EndAt = ct.Contest.EndAt,
                TeamId = ct.Team.Id,
                TeamName = ct.Team.TeamName,
                LeaderId = ct.Team.LeaderId,
                JoinedAt = ct.JoinAt,
                Rank = ct.Rank,
                Score = ct.Score,
                Solved = ct.SolvedProblem
            })
            .OrderByDescending(x => x.StartAt)
            .ToListAsync();
    }
}