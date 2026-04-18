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
}