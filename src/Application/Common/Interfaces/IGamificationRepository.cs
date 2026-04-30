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
    Task<List<Badge>> GetAllBadgesAsync();

    Task<Guid> CreateBadgeAsync(Badge badge);
    Task<bool> ExistsBadgeCodeAsync(string badgeCode);

    Task<Badge?> GetBadgeByIdAsync(Guid badgeId);
    Task UpdateBadgeAsync(Badge badge);

    Task<bool> IsBadgeUsedAsync(Guid badgeId);
    Task DeleteBadgeAsync(Badge badge);

    Task AddUserBadgeAsync(UserBadge badge);
    Task UpdateUserBadgeAsync(UserBadge badge);

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
    Task<List<(string ProblemTitle, string Difficulty, DateTime SolvedAt)>> GetSolvedProblemHistoryAsync(Guid userId);
    Task<List<(string ContestTitle, int Rank, DateTime JoinAt)>> GetContestResultHistoryAsync(Guid userId);

    // =========================
    // LEADERBOARD
    // =========================
    Task<List<(Guid UserId, int Value)>> GetLeaderboardAsync(string type, int top = 50);

    // =========================
    // DAILY ACTIVITIES
    // =========================
    Task<List<DailyActivityRaw>> GetDailyActivitiesAsync(Guid userId, int year);

    // =========================
    // DIFFICULTY STATS
    // =========================
    Task<Dictionary<string, int>> GetSolvedCountByDifficultyAsync(Guid userId);
    Task<Dictionary<string, int>> GetProblemCountByDifficultyAsync();

    // =========================
    // SAVE
    // =========================
    Task SaveChangesAsync();
}

public record DailyActivityRaw(DateOnly Date, int Count);