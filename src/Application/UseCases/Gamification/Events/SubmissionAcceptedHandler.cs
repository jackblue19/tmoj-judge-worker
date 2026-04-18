using Application.Common.Events;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Gamification.Events;

public class SubmissionAcceptedHandler
    : INotificationHandler<SubmissionAcceptedEvent>
{
    private readonly IGamificationRepository _repo;

    public SubmissionAcceptedHandler(IGamificationRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(SubmissionAcceptedEvent notification, CancellationToken ct)
    {
        var userId = notification.UserId;

        // =========================
        // COUNT SOLVED
        // =========================
        var solved = await _repo.GetSolvedProblemCountAsync(userId);

        // =========================
        // GET RULES
        // =========================
        var rules = await _repo.GetActiveRulesAsync();

        var solvedRules = rules
            .Where(r => r.RuleType == "solved_count");

        foreach (var rule in solvedRules)
        {
            // ❗ FIX Ở ĐÂY
            if (solved >= rule.TargetValue)
            {
                var userBadges = await _repo.GetUserBadgesAsync(userId);

                var already = userBadges
                    .Any(x => x.BadgeId == rule.BadgeId);

                if (!already)
                {
                    await _repo.AddUserBadgeAsync(new UserBadge
                    {
                        UserId = userId,
                        BadgeId = rule.BadgeId,
                        AwardedAt = DateTime.UtcNow,
                        ContextType = "problem"
                    });
                }
            }
        }

        await _repo.SaveChangesAsync();
    }
}