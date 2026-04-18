using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Common.Services;

public class StudyPlanApplicationService : IStudyPlanApplicationService
{
    private readonly IStudyPlanRepository _repo;

    public StudyPlanApplicationService(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task AddProblemAsync(Guid planId, Guid problemId)
    {
        await _repo.AddItemAsync(new StudyPlanItem
        {
            Id = Guid.NewGuid(),
            StudyPlanId = planId,
            ProblemId = problemId,
            OrderIndex = 0
        });

        await _repo.SaveChangesAsync();
    }
}