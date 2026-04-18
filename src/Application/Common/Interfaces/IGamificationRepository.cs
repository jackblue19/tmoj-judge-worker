using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IGamificationRepository
{
    // =========================
    // STREAK
    // =========================
    Task<UserStreak?> GetUserStreakAsync(Guid userId);
    Task CreateOrUpdateStreakAsync(UserStreak streak);

    // =========================
    // BADGES
    // =========================
    Task<List<UserBadge>> GetUserBadgesAsync(Guid userId);

    Task<Guid> CreateBadgeAsync(Badge badge);
    Task<bool> ExistsBadgeCodeAsync(string badgeCode);

    Task<Badge?> GetBadgeByIdAsync(Guid badgeId);
    Task UpdateBadgeAsync(Badge badge);

    Task<bool> IsBadgeUsedAsync(Guid badgeId);
    Task DeleteBadgeAsync(Badge badge);

    Task AddUserBadgeAsync(UserBadge badge);

    Task CreateBadgeRuleAsync(BadgeRule rule);
    Task<List<BadgeRule>> GetAllBadgeRulesAsync();

    Task<BadgeRule?> GetBadgeRuleByIdAsync(Guid id);
    Task DisableBadgeRuleAsync(Guid id);
    Task UpdateBadgeRuleAsync(BadgeRule rule);

    Task<bool> IsFirstAcceptedAsync(Guid userId, Guid problemId, Guid submissionId);
    Task<List<(Guid UserId, int Rank)>> GetContestRankingAsync(Guid contestId);
    // =========================
    // RULES
    // =========================
    Task<List<BadgeRule>> GetActiveRulesAsync();

    // =========================
    // PROGRESS (DERIVED)
    // =========================
    Task<int> GetSolvedProblemCountAsync(Guid userId);

    // =========================
    // HISTORY
    // =========================
    Task<List<UserBadge>> GetUserBadgeHistoryAsync(Guid userId);

    // =========================
    // LEADERBOARD
    // =========================
    Task<List<(Guid UserId, int Value)>> GetLeaderboardAsync(string type, int top = 50);

    // =========================
    // SAVE
    // =========================
    Task SaveChangesAsync();
}