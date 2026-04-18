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
    Task AddUserBadgeAsync(UserBadge badge);

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