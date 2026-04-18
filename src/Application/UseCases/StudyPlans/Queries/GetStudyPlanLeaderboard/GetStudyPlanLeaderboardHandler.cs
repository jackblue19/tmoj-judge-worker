using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanLeaderboard;

public class GetStudyPlanLeaderboardHandler
    : IRequestHandler<GetStudyPlanLeaderboardQuery, List<StudyPlanLeaderboardDto>>
{
    private readonly IStudyPlanRepository _repo;

    public GetStudyPlanLeaderboardHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<StudyPlanLeaderboardDto>> Handle(
        GetStudyPlanLeaderboardQuery request,
        CancellationToken ct)
    {
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);
        var totalItems = items.Count;

        if (totalItems == 0)
            return new List<StudyPlanLeaderboardDto>();

        var progresses = await _repo.GetAllProgressByPlanAsync(request.StudyPlanId);

        var grouped = progresses
            .GroupBy(x => x.UserId)
            .ToList();

        var result = grouped
            .Select(g =>
            {
                var completed = g.Count(x => x.IsCompleted ?? false);

                var lastActivity = g
                    .Where(x => x.CompletedAt != null)
                    .OrderByDescending(x => x.CompletedAt)
                    .FirstOrDefault()
                    ?.CompletedAt;

                return new StudyPlanLeaderboardDto
                {
                    UserId = g.Key,
                    CompletedItems = completed,
                    TotalItems = totalItems,
                    ProgressPercent = totalItems == 0
                        ? 0
                        : (double)completed / totalItems * 100,
                    LastActivityAt = lastActivity
                };
            })
            .OrderByDescending(x => x.ProgressPercent)
            .ThenByDescending(x => x.LastActivityAt)
            .ToList();

        return result;
    }
}