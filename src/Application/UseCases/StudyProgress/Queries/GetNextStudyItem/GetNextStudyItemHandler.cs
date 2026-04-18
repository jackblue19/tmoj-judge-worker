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
        // 1. get current item
        var currentItems = await _repo.GetItemsByPlanIdAsync(Guid.Empty);
        // NOTE: ta không biết planId ở đây → sẽ optimize phía dưới

        var currentProgress = await _repo.GetItemProgressAsync(
            request.UserId,
            request.StudyPlanItemId
        );

        // 2. get all items of this plan (FIX: cần resolve planId)
        var allProgress = await _repo.GetAllItemProgressByUserAsync(request.UserId);

        // 3. get current item
        var currentItem = await _repo.GetItemByIdAsync(request.StudyPlanItemId);
        if (currentItem == null)
            return null;

        var items = await _repo.GetItemsByPlanIdAsync(currentItem.StudyPlanId);

        var ordered = items
            .OrderBy(x => x.OrderIndex)
            .ToList();

        // 4. find next item
        for (int i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Id == request.StudyPlanItemId)
            {
                if (i + 1 < ordered.Count)
                    return ordered[i + 1].Id;

                return null;
            }
        }

        return null;
    }
}