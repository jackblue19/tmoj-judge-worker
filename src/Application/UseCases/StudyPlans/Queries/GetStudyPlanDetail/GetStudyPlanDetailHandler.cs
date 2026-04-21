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

        var ordered = items
            .OrderBy(x => x.OrderIndex)
            .ToList();

        // =========================
        // MAP DTO (NO PROGRESS HERE)
        // =========================
        var resultItems = ordered
            .Select(i => new StudyPlanItemDto
            {
                StudyPlanItemId = i.Id,
                ProblemId = i.ProblemId,
                Order = i.OrderIndex,

                // ❌ KHÔNG SET PROGRESS Ở ĐÂY
                IsCompleted = false,
                IsUnlocked = false
            })
            .ToList();

        return new StudyPlanDetailDto
        {
            Id = plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            Items = resultItems
        };
    }
}