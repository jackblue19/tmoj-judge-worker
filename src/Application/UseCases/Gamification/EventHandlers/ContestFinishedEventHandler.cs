using Application.Common.Events;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Gamification.EventHandlers;

public class ContestFinishedEventHandler
    : INotificationHandler<ContestFinishedEvent>
{
    private readonly IGamificationRepository _repo;

    public ContestFinishedEventHandler(
        IGamificationRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(
        ContestFinishedEvent request,
        CancellationToken ct)
    {
        // =========================
        // 1. GET RANKING
        // =========================
        var ranking = await _repo.GetContestRankingAsync(request.ContestId);

        if (ranking.Count == 0)
            return;

        // =========================
        // 2. GET RULES
        // =========================
        var rules = await _repo.GetActiveRulesAsync();

        // =========================
        // 3. APPLY RULES
        // =========================
        foreach (var rule in rules)
        {
            if (rule.RuleType != "rank")
                continue;

            foreach (var (userId, rank) in ranking)
            {
                if (rank <= rule.TargetValue)
                {
                    var userBadges = await _repo.GetUserBadgesAsync(userId);

                    var alreadyOwned = userBadges
                        .Any(x => x.BadgeId == rule.BadgeId);

                    if (!alreadyOwned)
                    {
                        await _repo.AddUserBadgeAsync(new UserBadge
                        {
                            UserBadgesId = Guid.NewGuid(),
                            UserId = userId,
                            BadgeId = rule.BadgeId,
                            AwardedAt = DateTime.UtcNow,
                            ContextType = "contest",
                            SourceId = request.ContestId
                        });
                    }
                }
            }
        }

        // =========================
        // 4. SAVE
        // =========================
        await _repo.SaveChangesAsync();
    }
}