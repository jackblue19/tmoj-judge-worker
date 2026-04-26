using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetDailyActivities;

public class GetDailyActivitiesHandler
    : IRequestHandler<GetDailyActivitiesQuery, List<DailyActivityDto>>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetDailyActivitiesHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<DailyActivityDto>> Handle(
        GetDailyActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var year = request.Year ?? DateTime.UtcNow.Year;

        var rawData = await _repo.GetDailyActivitiesAsync(userId, year);

        return rawData
            .Select(x => new DailyActivityDto
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                Count = x.Count
            })
            .OrderBy(x => x.Date)
            .ToList();
    }
}
