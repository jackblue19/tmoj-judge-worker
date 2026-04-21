using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.StudyProgress.Queries.GetNextStudyItem;

public class GetNextStudyItemHandler
    : IRequestHandler<GetNextStudyItemQuery, Guid?>
{
    private readonly IStudyPlanRepository _repo;

    public GetNextStudyItemHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<Guid?> Handle(GetNextStudyItemQuery request, CancellationToken ct)
    {
        // =========================
        // 1. GET CURRENT ITEM
        // =========================
        var currentItem = await _repo.GetItemByIdAsync(request.StudyPlanItemId);

        if (currentItem == null)
            return null;

        // =========================
        // 2. GET ALL ITEMS IN PLAN
        // =========================
        var items = await _repo.GetItemsByPlanIdAsync(currentItem.StudyPlanId);

        if (items == null || items.Count == 0)
            return null;

        var ordered = items
            .OrderBy(x => x.OrderIndex)
            .ToList();

        // =========================
        // 3. FIND NEXT ITEM
        // =========================
        for (int i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Id == request.StudyPlanItemId)
            {
                // nếu còn item phía sau
                if (i + 1 < ordered.Count)
                    return ordered[i + 1].Id;

                // nếu là item cuối
                return null;
            }
        }

        return null;
    }
}