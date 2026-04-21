using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetUnlockedPlans;

public class GetUnlockedPlansHandler
    : IRequestHandler<GetUnlockedPlansQuery, List<StudyPlanDto>>
{
    private readonly IStudyPlanRepository _repo;

    public GetUnlockedPlansHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<StudyPlanDto>> Handle(
        GetUnlockedPlansQuery request,
        CancellationToken ct)
    {
        // =========================
        // 1. GET ALL PLANS (FIX: bỏ CreatorId)
        // =========================
        var plans = await _repo.GetAllAsync();

        if (plans == null || plans.Count == 0)
            return new List<StudyPlanDto>();

        // =========================
        // 2. ORDER (tạm dùng CreatedAt)
        // =========================
        var orderedPlans = plans
            .OrderBy(x => x.CreatedAt)
            .ToList();

        // =========================
        // 3. BATCH CHECK COMPLETION
        // =========================
        var planIds = orderedPlans.Select(x => x.Id).ToList();

        var completedMap = await _repo.GetCompletedPlansAsync(
            request.UserId,
            planIds
        );

        // =========================
        // 4. BUILD RESULT (CHAIN LOGIC)
        // =========================
        var result = new List<StudyPlanDto>();

        for (int i = 0; i < orderedPlans.Count; i++)
        {
            var plan = orderedPlans[i];

            var isCompleted = completedMap.GetValueOrDefault(plan.Id);

            bool isUnlocked = i == 0
                ? true
                : completedMap.GetValueOrDefault(orderedPlans[i - 1].Id);

            result.Add(new StudyPlanDto
            {
                Id = plan.Id,
                Title = plan.Title,
                Order = i + 1,
                ProblemCount = plan.StudyPlanItems?.Count ?? 0,
                IsCompleted = isCompleted,
                IsUnlocked = isUnlocked
            });
        }

        return result;
    }
}