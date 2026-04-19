using Application.Common.Interfaces;
using Application.UseCases.StudyProgress.Dtos;
using MediatR;

namespace Application.UseCases.StudyProgress.Queries.GetMyStudyProgress;

public class GetMyStudyProgressHandler
    : IRequestHandler<GetMyStudyProgressQuery, MyStudyProgressDto>
{
    private readonly IStudyPlanRepository _repo;

    public GetMyStudyProgressHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<MyStudyProgressDto> Handle(
        GetMyStudyProgressQuery request,
        CancellationToken ct)
    {
        // 1. GET ALL PROGRESS (SAFE - no navigation dependency)
        var progresses = await _repo.GetAllItemProgressByUserAsync(request.UserId);

        if (progresses == null || progresses.Count == 0)
        {
            return new MyStudyProgressDto
            {
                Items = new List<MyStudyPlanProgressItemDto>()
            };
        }

        // 2. SAFE GROUP BY PLAN ID (no navigation property)
        var grouped = progresses
            .Where(x => x.StudyPlanItemId != Guid.Empty)
            .GroupBy(x => x.StudyPlanItem.StudyPlanId)
            .ToList();

        var result = new List<MyStudyPlanProgressItemDto>();

        foreach (var group in grouped)
        {
            var planId = group.Key;

            // DISTINCT ITEMS (IMPORTANT FIX)
            var distinctItemIds = group
                .Select(x => x.StudyPlanItemId)
                .Distinct()
                .ToList();

            var totalItems = distinctItemIds.Count;

            var completedItems = group
                .Where(x => x.IsCompleted ?? false)
                .Select(x => x.StudyPlanItemId)
                .Distinct()
                .Count();

            var percent = totalItems == 0
                ? 0
                : (double)completedItems / totalItems * 100;

            var first = group.FirstOrDefault();

            result.Add(new MyStudyPlanProgressItemDto
            {
                StudyPlanId = planId,
                Title = first?.StudyPlanItem?.StudyPlan?.Title ?? string.Empty,

                TotalItems = totalItems,
                CompletedItems = completedItems,
                ProgressPercent = percent,

                IsCompleted = totalItems > 0 && completedItems == totalItems
            });
        }

        return new MyStudyProgressDto
        {
            Items = result
        };
    }
}