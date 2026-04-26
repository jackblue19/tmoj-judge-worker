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
        var plans = await _repo.GetAllAsync();

        if (plans == null || plans.Count == 0)
            return new List<StudyPlanDto>();

        var orderedPlans = plans
            .OrderBy(x => x.CreatedAt)
            .ToList();

        var result = new List<StudyPlanDto>();

        foreach (var plan in orderedPlans)
        {
            var isUnlocked =
                !plan.IsPaid ||
                await _repo.HasUserPurchasedPlanAsync(request.UserId, plan.Id);

            var totalItems = await _repo.GetItemCountAsync(plan.Id);

            var progresses = await _repo.GetItemProgressByPlanAsync(
                request.UserId,
                plan.Id
            );

            var completed = progresses.Count(x => x.IsCompleted == true);

            var isCompleted =
                totalItems > 0 &&
                completed == totalItems;

            if (isUnlocked)
            {
                result.Add(new StudyPlanDto
                {
                    Id = plan.Id,
                    Title = plan.Title,
                    Order = 0,
                    ProblemCount = totalItems,
                    Price = plan.Price,
                    IsPaid = plan.IsPaid,

                    // 🔥 FIX NULLABLE HERE
                    IsCompleted = isCompleted,
                    IsUnlocked = isUnlocked
                });
            }
        }

        return result;
    }
}