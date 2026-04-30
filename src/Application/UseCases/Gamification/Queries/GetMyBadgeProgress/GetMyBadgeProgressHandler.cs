using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Queries.GetMyBadgeProgress;

public class GetMyBadgeProgressHandler
    : IRequestHandler<GetMyBadgeProgressQuery, List<BadgeProgressDto>>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyBadgeProgressHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<BadgeProgressDto>> Handle(
        GetMyBadgeProgressQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? request.UserId;

        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("Không xác định được người dùng.");

        // ===== DATA =====
        var rules = await _repo.GetActiveRulesAsync();
        var userBadges = await _repo.GetUserBadgesAsync(userId);
        var solvedCount = await _repo.GetSolvedProblemCountAsync(userId);
        var streak = await _repo.GetUserStreakAsync(userId);

        var result = new List<BadgeProgressDto>();
        bool needsSave = false;

        foreach (var rule in rules)
        {
            var badge = rule.Badge; // navigation
            if (badge == null) continue;

            int current = 0;

            // ===== CALCULATE PROGRESS =====
            switch (rule.RuleType.ToLower())
            {
                case "solved_count":
                    current = solvedCount;
                    break;

                case "streak":
                case "streak_days":
                    current = streak?.CurrentStreak ?? 0;
                    break;

                case "rank":
                    current = 0; // chưa làm contest ranking
                    break;
            }

            var userBadge = userBadges.FirstOrDefault(x => x.BadgeId == badge.BadgeId);
            var isEarned = userBadge != null;
            bool isNotified = false;

            if (userBadge != null && !string.IsNullOrEmpty(userBadge.MetaJson) && userBadge.MetaJson.Contains("\"isNotified\":true"))
            {
                isNotified = true;
            }
            
            // 🔥 Tự động "bù" Huy hiệu nếu đã đạt mà chưa có (Self-healing)
            if (!isEarned && current >= rule.TargetValue && rule.TargetValue > 0)
            {
                var newBadge = new UserBadge
                {
                    UserBadgesId = Guid.NewGuid(),
                    UserId = userId,
                    BadgeId = badge.BadgeId,
                    AwardedAt = DateTime.UtcNow
                };
                await _repo.AddUserBadgeAsync(newBadge);
                isEarned = true;
                needsSave = true;
            }

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
                IsCompleted = isEarned,
                IsNotified = isNotified
            });
        }

        if (needsSave)
        {
            await _repo.SaveChangesAsync();
        }

        return result;
    }
}
