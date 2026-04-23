using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class GamificationRepository : IGamificationRepository
{
    private readonly TmojDbContext _db;

    public GamificationRepository(TmojDbContext db)
    {
        _db = db;
    }

    // =========================
    // STREAK
    // =========================
    public async Task<UserStreak?> GetUserStreakAsync(Guid userId)
    {
        return await _db.Set<UserStreak>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task CreateOrUpdateStreakAsync(UserStreak streak)
    {
        var existing = await _db.Set<UserStreak>()
            .FirstOrDefaultAsync(x => x.UserId == streak.UserId);

        if (existing == null)
        {
            await _db.Set<UserStreak>().AddAsync(streak);
        }
        else
        {
            existing.CurrentStreak = streak.CurrentStreak;
            existing.LongestStreak = streak.LongestStreak;
            existing.LastActiveDate = streak.LastActiveDate;
        }
    }

    // =========================
    // BADGES
    // =========================
    public async Task<List<UserBadge>> GetUserBadgesAsync(Guid userId)
    {
        return await _db.Set<UserBadge>()
            .AsNoTracking()
            .Include(x => x.Badge)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AwardedAt)
            .ToListAsync();
    }

    public async Task AddUserBadgeAsync(UserBadge badge)
    {
        await _db.Set<UserBadge>().AddAsync(badge);
    }

    public async Task<List<Badge>> GetAllBadgesAsync()
    {
        return await _db.Set<Badge>()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsBadgeCodeAsync(string badgeCode)
    {
        return await _db.Set<Badge>()
            .AnyAsync(x => x.BadgeCode == badgeCode);
    }

    public async Task<Guid> CreateBadgeAsync(Badge badge)
    {
        await _db.Set<Badge>().AddAsync(badge);
        await _db.SaveChangesAsync();

        return badge.BadgeId;
    }

    public async Task<Badge?> GetBadgeByIdAsync(Guid badgeId)
    {
        return await _db.Set<Badge>()
            .FirstOrDefaultAsync(x => x.BadgeId == badgeId);
    }

    public Task UpdateBadgeAsync(Badge badge)
    {
        _db.Set<Badge>().Update(badge);
        return Task.CompletedTask;
    }

    public async Task<bool> IsBadgeUsedAsync(Guid badgeId)
    {
        return await _db.Set<UserBadge>()
            .AnyAsync(x => x.BadgeId == badgeId);
    }

    public Task DeleteBadgeAsync(Badge badge)
    {
        _db.Set<Badge>().Remove(badge);
        return Task.CompletedTask;
    }

    // =========================
    // 🔥 BADGE RULES (NEW)
    // =========================
    public async Task CreateBadgeRuleAsync(BadgeRule rule)
    {
        await _db.Set<BadgeRule>().AddAsync(rule);
    }

    public async Task<List<BadgeRule>> GetAllBadgeRulesAsync()
    {
        return await _db.Set<BadgeRule>()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<BadgeRule?> GetBadgeRuleByIdAsync(Guid id)
    {
        return await _db.Set<BadgeRule>()
            .FirstOrDefaultAsync(x => x.BadgeRulesId == id);
    }

    public Task UpdateBadgeRuleAsync(BadgeRule rule)
    {
        _db.Set<BadgeRule>().Update(rule);
        return Task.CompletedTask;
    }

    // disable = set IsActive = false (soft delete)
    public async Task DisableBadgeRuleAsync(Guid id)
    {
        var rule = await _db.Set<BadgeRule>()
            .FirstOrDefaultAsync(x => x.BadgeRulesId == id);

        if (rule != null)
        {
            rule.IsActive = false;
            rule.UpdatedAt = DateTime.UtcNow;
        }
    }

    // =========================
    // RULES (ACTIVE ONLY)
    // =========================
    public async Task<List<BadgeRule>> GetActiveRulesAsync()
    {
        return await _db.Set<BadgeRule>()
            .AsNoTracking()
            .Include(x => x.Badge)
            .Where(x => x.IsActive)
            .ToListAsync();
    }

    // =========================
    // PROGRESS
    // =========================
    public async Task<int> GetSolvedProblemCountAsync(Guid userId)
    {
        return await _db.Set<UserProblemStat>()
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.Solved);
    }

    // =========================
    // HISTORY
    // =========================
    public async Task<List<UserBadge>> GetUserBadgeHistoryAsync(Guid userId)
    {
        return await _db.Set<UserBadge>()
            .AsNoTracking()
            .Include(x => x.Badge)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AwardedAt)
            .ToListAsync();
    }

    public async Task<List<(string ProblemTitle, string Difficulty, DateTime SolvedAt)>> GetSolvedProblemHistoryAsync(Guid userId)
    {
        var data = await _db.Set<UserProblemStat>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Solved)
            .Join(
                _db.Set<Problem>(),
                ups => ups.ProblemId,
                p => p.Id,
                (ups, p) => new
                {
                    ProblemTitle = p.Title,
                    Difficulty = p.Difficulty ?? "unknown",
                    SolvedAt = ups.LastSubmissionAt ?? DateTime.MinValue
                }
            )
            .OrderByDescending(x => x.SolvedAt)
            .ToListAsync();

        return data
            .Select(x => (x.ProblemTitle, x.Difficulty, x.SolvedAt))
            .ToList();
    }

    public async Task<List<(string ContestTitle, int Rank, DateTime JoinAt)>> GetContestResultHistoryAsync(Guid userId)
    {
        var data = await _db.Set<ContestTeam>()
            .AsNoTracking()
            .Include(ct => ct.Contest)
            .Include(ct => ct.Team)
                .ThenInclude(t => t.TeamMembers)
            .Where(ct => ct.Team.TeamMembers.Any(tm => tm.UserId == userId))
            .OrderByDescending(ct => ct.JoinAt)
            .Select(ct => new
            {
                ContestTitle = ct.Contest.Title,
                Rank = ct.Rank ?? 0,
                JoinAt = ct.JoinAt
            })
            .ToListAsync();

        return data
            .Select(x => (x.ContestTitle, x.Rank, x.JoinAt))
            .ToList();
    }

    // =========================
    // 🔥 LEADERBOARD (FIX EF)
    // =========================
    public async Task<List<(Guid UserId, int Value)>> GetLeaderboardAsync(string type, int top = 50)
    {
        type = type?.ToLower() ?? "exp";

        List<LeaderboardRawDto> raw;

        if (type == "streak")
        {
            raw = await _db.Set<UserStreak>()
                .AsNoTracking()
                .OrderByDescending(x => x.CurrentStreak)
                .Take(top)
                .Select(x => new LeaderboardRawDto
                {
                    UserId = x.UserId,
                    Value = x.CurrentStreak ?? 0
                })
                .ToListAsync();
        }
        else if (type == "badge")
        {
            raw = await _db.Set<UserBadge>()
                .AsNoTracking()
                .GroupBy(x => x.UserId)
                .Select(g => new LeaderboardRawDto
                {
                    UserId = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .Take(top)
                .ToListAsync();
        }
        else
        {
            raw = await _db.Set<UserProblemStat>()
                .AsNoTracking()
                .Where(x => x.Solved)
                .GroupBy(x => x.UserId)
                .Select(g => new LeaderboardRawDto
                {
                    UserId = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .Take(top)
                .ToListAsync();
        }

        return raw
            .Select(x => (x.UserId, x.Value))
            .ToList();
    }

    // =========================
    // SAVE
    // =========================
    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsFirstAcceptedAsync(
    Guid userId,
    Guid problemId,
    Guid submissionId)
    {
        return !await _db.Submissions
            .AnyAsync(x =>
                x.UserId == userId &&
                x.ProblemId == problemId &&
                x.VerdictCode == "ac" &&
                x.Id != submissionId);
    }

    public async Task<List<(Guid UserId, int Rank)>> GetContestRankingAsync(Guid contestId)
    {
        var ranking = await _db.Submissions
            .AsNoTracking()
            .Include(x => x.ContestProblem)
            .Where(x =>
                x.ContestProblemId != null &&
                x.ContestProblem!.ContestId == contestId &&
                x.VerdictCode == "ac")
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Score = g.Select(x => x.ProblemId).Distinct().Count()
            })
            .OrderByDescending(x => x.Score)
            .ToListAsync();

        var result = new List<(Guid, int)>();

        for (int i = 0; i < ranking.Count; i++)
        {
            result.Add((ranking[i].UserId, i + 1));
        }

        return result;
    }

    // =========================
    // DAILY ACTIVITIES
    // =========================
    public async Task<List<DailyActivityRaw>> GetDailyActivitiesAsync(Guid userId, int year)
    {
        var startDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 1) Submissions
        var submissionDates = await _db.Submissions
            .AsNoTracking()
            .Where(x => x.UserId == userId
                && x.CreatedAt >= startDate
                && x.CreatedAt < endDate)
            .Select(x => x.CreatedAt.Date)
            .ToListAsync();

        // 2) UserSessions (login)
        var sessionDates = await _db.UserSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId
                && x.CreatedAt >= startDate
                && x.CreatedAt < endDate)
            .Select(x => x.CreatedAt.Date)
            .ToListAsync();

        // 3) Contest participation (via ContestTeams → TeamMembers)
        var contestDates = await _db.Set<ContestTeam>()
            .AsNoTracking()
            .Include(ct => ct.Team)
                .ThenInclude(t => t.TeamMembers)
            .Where(ct => ct.Team.TeamMembers.Any(tm => tm.UserId == userId)
                && ct.JoinAt >= startDate
                && ct.JoinAt < endDate)
            .Select(ct => ct.JoinAt.Date)
            .ToListAsync();

        // Union all + group by date
        var allDates = submissionDates
            .Concat(sessionDates)
            .Concat(contestDates);

        var grouped = allDates
            .GroupBy(d => DateOnly.FromDateTime(d))
            .Select(g => new DailyActivityRaw(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        return grouped;
    }

    // =========================
    // DIFFICULTY STATS
    // =========================
    public async Task<Dictionary<string, int>> GetSolvedCountByDifficultyAsync(Guid userId)
    {
        var result = await _db.Set<UserProblemStat>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Solved)
            .Join(
                _db.Set<Problem>(),
                ups => ups.ProblemId,
                p => p.Id,
                (ups, p) => p.Difficulty
            )
            .Where(d => d != null)
            .GroupBy(d => d!)
            .Select(g => new { Difficulty = g.Key, Count = g.Count() })
            .ToListAsync();

        return result.ToDictionary(x => x.Difficulty, x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetProblemCountByDifficultyAsync()
    {
        var result = await _db.Set<Problem>()
            .AsNoTracking()
            .Where(p => p.IsActive && p.Difficulty != null)
            .GroupBy(p => p.Difficulty!)
            .Select(g => new { Difficulty = g.Key, Count = g.Count() })
            .ToListAsync();

        return result.ToDictionary(x => x.Difficulty, x => x.Count);
    }
}