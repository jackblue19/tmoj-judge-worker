using Application.Common.Events;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Gamification.EventHandlers;

public class DailyLoginEventHandler : INotificationHandler<DailyLoginEvent>
{
    private readonly IGamificationRepository _repo;
    private readonly ILogger<DailyLoginEventHandler> _logger;

    public DailyLoginEventHandler(
        IGamificationRepository repo,
        ILogger<DailyLoginEventHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Handle(DailyLoginEvent notification, CancellationToken ct)
    {
        var userId = notification.UserId;

        // 🔥 Convert đúng kiểu
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var streak = await _repo.GetUserStreakAsync(userId);

        if (streak == null)
        {
            _logger.LogInformation("Create new streak for user {UserId}", userId);

            streak = new UserStreak
            {
                UserId = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastActiveDate = today
            };

            await _repo.CreateOrUpdateStreakAsync(streak);
            await _repo.SaveChangesAsync();
            return;
        }

        var last = streak.LastActiveDate;

        // =========================
        // SAME DAY → ignore
        // =========================
        if (last.HasValue && last.Value == today)
        {
            _logger.LogInformation("User {UserId} already logged today", userId);
            return;
        }

        // =========================
        // CONTINUE STREAK
        // =========================
        if (last.HasValue && last.Value == today.AddDays(-1))
        {
            streak.CurrentStreak += 1;

            if (streak.CurrentStreak > streak.LongestStreak)
            {
                streak.LongestStreak = streak.CurrentStreak;
            }
        }
        else
        {
            // =========================
            // RESET STREAK
            // =========================
            _logger.LogInformation("Reset streak for user {UserId}", userId);

            streak.CurrentStreak = 1;
        }

        streak.LastActiveDate = today;

        await _repo.CreateOrUpdateStreakAsync(streak);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Updated streak for user {UserId}: {Streak}", userId, streak.CurrentStreak);

        // =========================
        // 🔥 AUTO-AWARD BADGES
        // =========================
        var rules = await _repo.GetActiveRulesAsync();
        var userBadges = await _repo.GetUserBadgesAsync(userId);
        var currentStreak = streak.CurrentStreak ?? 0;

        foreach (var rule in rules.Where(r => r.RuleType == "streak" || r.RuleType == "streak_days"))
        {
            if (currentStreak >= rule.TargetValue)
            {
                // Kiểm tra xem đã có huy hiệu này chưa
                if (!userBadges.Any(b => b.BadgeId == rule.BadgeId))
                {
                    _logger.LogInformation("Awarding badge {BadgeName} to user {UserId}", rule.Badge?.Name, userId);
                    
                    var newBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = rule.BadgeId,
                        AwardedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                    };

                    await _repo.AddUserBadgeAsync(newBadge);
                }
            }
        }
        
        await _repo.SaveChangesAsync();
    }
}