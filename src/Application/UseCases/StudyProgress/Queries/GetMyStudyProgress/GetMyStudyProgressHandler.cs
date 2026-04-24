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
        // =========================
        // 1. GET ALL PROGRESS
        // =========================
        var progresses = await _repo.GetAllItemProgressByUserAsync(request.UserId);

        if (progresses == null || progresses.Count == 0)
        {
            return new MyStudyProgressDto
            {
                Items = new List<MyStudyPlanProgressItemDto>()
            };
        }

        // =========================
        // 2. GET ITEM IDS
        // =========================
        var itemIds = progresses
            .Select(x => x.StudyPlanItemId)
            .Distinct()
            .ToList();

        // =========================
        // 3. MAP ITEM -> PLAN AND GET TITLES & COUNTS
        // =========================
        var mapping = await _repo.GetItemPlanMappingAsync(itemIds);
        
        var planIds = mapping.Values.Distinct().ToList();
        var titles = await _repo.GetPlanTitlesAsync(planIds);
        
        // Dictionary to hold total items per plan
        var totalItemsDict = new Dictionary<Guid, int>();
        foreach (var pId in planIds)
        {
            totalItemsDict[pId] = await _repo.GetItemCountAsync(pId);
        }

        // =========================
        // 4. GROUP BY PLAN
        // =========================
        var grouped = progresses
            .Where(x => mapping.ContainsKey(x.StudyPlanItemId))
            .GroupBy(x => mapping[x.StudyPlanItemId])
            .ToList();

        var result = new List<MyStudyPlanProgressItemDto>();

        // =========================
        // 5. BUILD RESULT
        // =========================
        foreach (var group in grouped)
        {
            var planId = group.Key;
            
            var totalItems = totalItemsDict.GetValueOrDefault(planId, 0);

            var completedItems = group
                .Where(x => x.IsCompleted == true)
                .Select(x => x.StudyPlanItemId)
                .Distinct()
                .Count();

            var percent = totalItems == 0
                ? 0
                : (double)completedItems / totalItems * 100;

            result.Add(new MyStudyPlanProgressItemDto
            {
                StudyPlanId = planId,
                Title = titles.GetValueOrDefault(planId, "Unknown Plan"), // ✅ Lấy tên plan
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