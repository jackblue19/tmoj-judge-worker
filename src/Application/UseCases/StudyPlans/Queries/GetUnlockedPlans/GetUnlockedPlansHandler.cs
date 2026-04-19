using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;
using StudyPlanEntity = Domain.Entities.StudyPlan;

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
        // ✅ FIX: dùng đúng method hiện tại
        var plans = await _repo.GetByCreatorAsync(request.CreatorId);

        var result = new List<StudyPlanDto>();

        StudyPlanEntity? previous = null;

        foreach (var plan in plans)
        {
            bool isUnlocked = true;

            if (previous is not null)
            {
                isUnlocked = await _repo.IsStudyPlanCompletedAsync(
                    request.UserId,
                    previous.Id
                );
            }

            var isCompleted = await _repo.IsStudyPlanCompletedAsync(
                request.UserId,
                plan.Id
            );

            result.Add(new StudyPlanDto
            {
                Id = plan.Id,
                Title = plan.Title,
                Order = 0,
                ProblemCount = plan.StudyPlanItems?.Count ?? 0,
                IsCompleted = isCompleted,
                IsUnlocked = isUnlocked
            });

            previous = plan;
        }

        return result;
    }
}