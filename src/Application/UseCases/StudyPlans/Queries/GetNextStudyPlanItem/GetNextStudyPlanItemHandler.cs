using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetNextStudyPlanItem;

public class GetNextStudyPlanItemHandler
    : IRequestHandler<GetNextStudyPlanItemQuery, NextStudyPlanItemDto>
{
    private readonly IStudyPlanRepository _repo;

    public GetNextStudyPlanItemHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<NextStudyPlanItemDto> Handle(
        GetNextStudyPlanItemQuery request,
        CancellationToken ct)
    {
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);

        if (items == null || items.Count == 0)
        {
            return new NextStudyPlanItemDto
            {
                HasNext = false,
                Message = "Study plan has no items"
            };
        }

        var ordered = items
            .OrderBy(x => x.OrderIndex)
            .ToList();

        // ⚠️ FIX: dùng request.StudyPlanItemId (KHÔNG phải cái khác)
        var currentIndex = ordered.FindIndex(x => x.Id == request.StudyPlanItemId);

        if (currentIndex == -1)
        {
            return new NextStudyPlanItemDto
            {
                HasNext = false,
                Message = "Current item not found in study plan"
            };
        }

        var nextIndex = currentIndex + 1;

        if (nextIndex >= ordered.Count)
        {
            return new NextStudyPlanItemDto
            {
                HasNext = false,
                Message = "This is the last item in the study plan"
            };
        }

        var next = ordered[nextIndex];

        return new NextStudyPlanItemDto
        {
            HasNext = true,
            Message = "Next item found",
            NextItemId = next.Id,
            ProblemId = next.ProblemId,
            Order = next.OrderIndex
        };
    }
}