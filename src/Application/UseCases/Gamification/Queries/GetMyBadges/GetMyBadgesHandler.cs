using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyBadges;

public class GetMyBadgesHandler
    : IRequestHandler<GetMyBadgesQuery, List<UserBadgeDto>>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyBadgesHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<UserBadgeDto>> Handle(
    GetMyBadgesQuery request,
    CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = _currentUser.UserId.Value;

        var userBadges = await _repo.GetUserBadgesAsync(userId);

        return userBadges.Select(ub => new UserBadgeDto
        {
            UserBadgeId = ub.UserBadgesId,
            BadgeId = ub.BadgeId,

            Name = ub.Badge.Name,
            IconUrl = ub.Badge.IconUrl,
            Description = ub.Badge.Description,

            AwardedAt = ub.AwardedAt
        }).ToList();
    }
}