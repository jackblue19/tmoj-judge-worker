using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyStreak;

public class GetMyStreakHandler : IRequestHandler<GetMyStreakQuery, StreakDto>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyStreakHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<StreakDto> Handle(GetMyStreakQuery request, CancellationToken ct)
    {
        // ✅ Fix Guid?
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException();

        var userId = _currentUser.UserId.Value;

        var streak = await _repo.GetUserStreakAsync(userId);

        return new StreakDto
        {
            // ✅ Fix int?
            CurrentStreak = streak?.CurrentStreak ?? 0,
            LongestStreak = streak?.LongestStreak ?? 0,

            // ✅ Fix DateOnly -> DateTime
            LastActiveDate = streak?.LastActiveDate.HasValue == true
                ? streak.LastActiveDate.Value.ToDateTime(TimeOnly.MinValue)
                : null
        };
    }
}