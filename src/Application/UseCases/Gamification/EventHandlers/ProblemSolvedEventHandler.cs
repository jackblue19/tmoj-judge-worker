using Application.Common.Events;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Gamification.EventHandlers;

public class ProblemSolvedEventHandler
    : INotificationHandler<ProblemSolvedEvent>
{
    private readonly IGamificationRepository _repo;

    public ProblemSolvedEventHandler(
        IGamificationRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(
        ProblemSolvedEvent request,
        CancellationToken ct)
    {
        var userId = request.UserId;

        // =========================
        // 1. FIRST AC CHECK
        // =========================
        var isFirstAccepted = await _repo.IsFirstAcceptedAsync(
            request.UserId,
            request.ProblemId,
            request.SubmissionId);

        if (!isFirstAccepted)
            return;

        // =========================
        // 2. GET USER DATA
        // =========================
        var solvedCount = await _repo.GetSolvedProblemCountAsync(userId);

        var rules = await _repo.GetActiveRulesAsync();

        var userBadges = await _repo.GetUserBadgesAsync(userId);

        // =========================
        // 3. APPLY BADGE RULES
        // =========================
        foreach (var rule in rules)
        {
            if (rule.RuleType != "solved")
                continue;

            if (solvedCount >= rule.TargetValue)
            {
                var alreadyOwned = userBadges
                    .Any(x => x.BadgeId == rule.BadgeId);

                if (!alreadyOwned)
                {
                    await _repo.AddUserBadgeAsync(new UserBadge
                    {
                        UserBadgesId = Guid.NewGuid(), 
                        UserId = userId,
                        BadgeId = rule.BadgeId,
                        AwardedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // =========================
        // 4. SAVE
        // =========================
        await _repo.SaveChangesAsync();
    }
}