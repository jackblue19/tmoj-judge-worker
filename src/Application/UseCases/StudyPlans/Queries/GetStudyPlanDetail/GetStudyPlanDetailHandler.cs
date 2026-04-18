using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanDetail;

public class GetStudyPlanDetailHandler
    : IRequestHandler<GetStudyPlanDetailQuery, StudyPlanDetailDto>
{
    private readonly IStudyPlanRepository _repo;

    public GetStudyPlanDetailHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<StudyPlanDetailDto> Handle(
        GetStudyPlanDetailQuery request,
        CancellationToken ct)
    {
        // =========================
        // GET PLAN
        // =========================
        var plan = await _repo.GetByIdAsync(request.StudyPlanId);

        if (plan == null)
            throw new Exception("StudyPlan not found");

        // =========================
        // GET ITEMS
        // =========================
        var items = await _repo.GetItemsByPlanIdAsync(plan.Id);

        // =========================
        // GET ITEM PROGRESS (FIXED)
        // =========================
        var progress = await _repo.GetItemProgressByPlanAsync(
            request.UserId,
            plan.Id
        );

        // map theo StudyPlanItemId (FAST LOOKUP)
        var progressDict = progress
            .GroupBy(x => x.StudyPlanItemId)
            .ToDictionary(g => g.Key, g => g.First());

        // sort items
        var ordered = items
            .OrderBy(x => x.OrderIndex)
            .ToList();

        var resultItems = new List<StudyPlanItemDto>();

        for (int i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];

            progressDict.TryGetValue(item.Id, out var p);

            bool isCompleted = p?.IsCompleted == true;

            // =========================
            // UNLOCK LOGIC (CHAIN)
            // =========================
            bool isUnlocked = i == 0
                || resultItems[i - 1].IsCompleted;

            resultItems.Add(new StudyPlanItemDto
            {
                ProblemId = item.ProblemId,
                Order = item.OrderIndex,
                IsCompleted = isCompleted,
                IsUnlocked = isUnlocked
            });
        }

        return new StudyPlanDetailDto
        {
            Id = plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            Items = resultItems
        };
    }
}