using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlans;

public class GetStudyPlansHandler
    : IRequestHandler<GetStudyPlansQuery, List<StudyPlanDto>>
{
    private readonly IStudyPlanRepository _repo;

    public GetStudyPlansHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<StudyPlanDto>> Handle(
        GetStudyPlansQuery request,
        CancellationToken ct)
    {
        // =========================
        // GET DATA
        // =========================
        var plans = request.CreatorId.HasValue
            ? await _repo.GetByCreatorAsync(request.CreatorId.Value)
            : await _repo.GetAllAsync();

        // =========================
        // MAP DTO
        // =========================
        return plans
            .Select(p => new StudyPlanDto
            {
                Id = p.Id,
                Title = p.Title,
                Order = 0,
                ProblemCount = p.StudyPlanItems?.Count ?? 0,
                IsCompleted = false,
                IsUnlocked = true
            })
            .ToList();
    }
}