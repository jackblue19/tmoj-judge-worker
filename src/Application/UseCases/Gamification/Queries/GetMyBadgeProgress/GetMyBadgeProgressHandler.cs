using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyBadgeProgress;

public class GetMyBadgeProgressHandler
    : IRequestHandler<GetMyBadgeProgressQuery, List<BadgeProgressDto>>
{
    private readonly IGamificationRepository _repo;

    public GetMyBadgeProgressHandler(IGamificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<BadgeProgressDto>> Handle(
        GetMyBadgeProgressQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        // ===== DATA =====
        var rules = await _repo.GetActiveRulesAsync();
        var userBadges = await _repo.GetUserBadgesAsync(userId);
        var solvedCount = await _repo.GetSolvedProblemCountAsync(userId);
        var streak = await _repo.GetUserStreakAsync(userId);

        var result = new List<BadgeProgressDto>();

        foreach (var rule in rules)
        {
            var badge = rule.Badge; // navigation

            if (badge == null) continue;

            int current = 0;

            // ===== CALCULATE PROGRESS =====
            switch (rule.RuleType)
            {
                case "solved_count":
                    current = solvedCount;
                    break;

                case "streak_days":
                    current = streak?.CurrentStreak ?? 0;
                    break;

                case "rank":
                    current = 0; // chưa làm contest ranking
                    break;
            }

            var isEarned = userBadges.Any(x => x.BadgeId == badge.BadgeId);

            var percent = rule.TargetValue == 0
                ? 0
                : (double)current / rule.TargetValue * 100;

            result.Add(new BadgeProgressDto
            {
                BadgeId = badge.BadgeId,
                Name = badge.Name,
                IconUrl = badge.IconUrl,

                CurrentValue = current,
                TargetValue = rule.TargetValue,

                ProgressPercent = Math.Min(percent, 100),
                IsCompleted = isEarned
            });
        }

        return result;
    }
}