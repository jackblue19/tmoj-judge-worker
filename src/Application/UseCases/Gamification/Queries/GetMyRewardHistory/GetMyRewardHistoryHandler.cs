using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyRewardHistory;

public class GetMyRewardHistoryHandler
    : IRequestHandler<GetMyRewardHistoryQuery, List<RewardHistoryDto>>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyRewardHistoryHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<RewardHistoryDto>> Handle(
        GetMyRewardHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var result = new List<RewardHistoryDto>();

        // =========================
        // BADGE HISTORY
        // =========================
        var badges = await _repo.GetUserBadgeHistoryAsync(userId);

        result.AddRange(badges.Select(x => new RewardHistoryDto
        {
            Type = "badge",
            Title = x.Badge.Name,
            Description = x.Badge.Description,
            CreatedAt = x.AwardedAt
        }));

        // =========================
        // TODO (future):
        // coin / exp nếu có table riêng
        // =========================

        return result
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }
}