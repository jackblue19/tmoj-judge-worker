using Application.Common.Interfaces;
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
            .Include(x => x.Badge)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AwardedAt)
            .ToListAsync();
    }

    public async Task AddUserBadgeAsync(UserBadge badge)
    {
        await _db.Set<UserBadge>().AddAsync(badge);
    }

    // =========================
    // RULES
    // =========================
    public async Task<List<BadgeRule>> GetActiveRulesAsync()
    {
        return await _db.Set<BadgeRule>()
            .Where(x => x.IsActive)
            .ToListAsync();
    }

    // =========================
    // PROGRESS (DERIVED)
    // =========================
    public async Task<int> GetSolvedProblemCountAsync(Guid userId)
    {
        return await _db.Set<UserProblemStat>()
            .CountAsync(x => x.UserId == userId && x.Solved);
    }

    // =========================
    // HISTORY
    // =========================
    public async Task<List<UserBadge>> GetUserBadgeHistoryAsync(Guid userId)
    {
        return await _db.Set<UserBadge>()
            .Include(x => x.Badge)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AwardedAt)
            .ToListAsync();
    }

    // =========================
    // LEADERBOARD
    // =========================
    public async Task<List<(Guid UserId, int Value)>> GetLeaderboardAsync(string type, int top = 50)
    {
        if (type == "streak")
        {
            return await _db.Set<UserStreak>()
                .OrderByDescending(x => x.CurrentStreak)
                .Take(top)
                .Select(x => new ValueTuple<Guid, int>(
                    x.UserId,
                    x.CurrentStreak ?? 0
                ))
                .ToListAsync();
        }

        if (type == "badge")
        {
            return await _db.Set<UserBadge>()
                .GroupBy(x => x.UserId)
                .Select(g => new ValueTuple<Guid, int>(
                    g.Key,
                    g.Count()
                ))
                .OrderByDescending(x => x.Item2)
                .Take(top)
                .ToListAsync();
        }

        // default: exp = solved problems
        return await _db.Set<UserProblemStat>()
            .Where(x => x.Solved)
            .GroupBy(x => x.UserId)
            .Select(g => new ValueTuple<Guid, int>(
                g.Key,
                g.Count()
            ))
            .OrderByDescending(x => x.Item2)
            .Take(top)
            .ToListAsync();
    }

    // =========================
    // SAVE
    // =========================
    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}