using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanStats;

public class GetStudyPlanStatsHandler
    : IRequestHandler<GetStudyPlanStatsQuery, StudyPlanStatsDto>
{
    private readonly IStudyPlanRepository _repo;

    public GetStudyPlanStatsHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<StudyPlanStatsDto> Handle(GetStudyPlanStatsQuery request, CancellationToken ct)
    {
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);
        var totalItems = items.Count;

        if (totalItems == 0)
        {
            return new StudyPlanStatsDto
            {
                StudyPlanId = request.StudyPlanId,
                TotalItems = 0,
                TotalUsersEnrolled = 0,
                TotalCompletedUsers = 0,
                CompletionRate = 0,
                AverageProgress = 0
            };
        }

        var itemIds = items.Select(x => x.Id).ToList();

        var progresses = await _repo.GetAllProgressByPlanAsync(request.StudyPlanId);

        var groupByUser = progresses
            .GroupBy(x => x.UserId)
            .ToList();

        var totalUsers = groupByUser.Count;

        var completedUsers = groupByUser.Count(g =>
            g.All(p =>
                itemIds.Contains(p.StudyPlanItemId)
                && (p.IsCompleted ?? false)
            )
        );

        var avgProgress = totalUsers == 0
            ? 0
            : groupByUser.Average(g =>
                g.Count(p =>
                    itemIds.Contains(p.StudyPlanItemId)
                    && (p.IsCompleted ?? false)
                ) / (double)totalItems * 100
            );

        return new StudyPlanStatsDto
        {
            StudyPlanId = request.StudyPlanId,
            TotalItems = totalItems,
            TotalUsersEnrolled = totalUsers,
            TotalCompletedUsers = completedUsers,
            CompletionRate = totalUsers == 0
                ? 0
                : (double)completedUsers / totalUsers * 100,
            AverageProgress = avgProgress
        };
    }
}