using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyGamification;

public class GetMyGamificationHandler
    : IRequestHandler<GetMyGamificationQuery, GetMyGamificationResponse>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyGamificationHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<GetMyGamificationResponse> Handle(
        GetMyGamificationQuery request,
        CancellationToken cancellationToken)
    {
        // ✅ FIX NULLABLE GUID (SAFE)
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // =========================
        // GET DATA
        // =========================
        var streak = await _repo.GetUserStreakAsync(userId);
        var badges = await _repo.GetUserBadgesAsync(userId);
        var solvedCount = await _repo.GetSolvedProblemCountAsync(userId);

        // =========================
        // MAP
        // =========================
        return new GetMyGamificationResponse
        {
            CurrentStreak = streak?.CurrentStreak ?? 0,
            LongestStreak = streak?.LongestStreak ?? 0,
            SolvedProblems = solvedCount,

            Badges = badges.Select(b => new BadgeDto
            {
                BadgeId = b.BadgeId,
                Name = b.Badge.Name,
                IconUrl = b.Badge.IconUrl,
                Level = b.Badge.BadgeLevel
            }).ToList()
        };
    }
}