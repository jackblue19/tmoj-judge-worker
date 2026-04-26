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
        var plans = request.CreatorId.HasValue
            ? await _repo.GetByCreatorAsync(request.CreatorId.Value)
            : await _repo.GetAllAsync();

        if (plans == null || plans.Count == 0)
            return new List<StudyPlanDto>();

        var ordered = plans
            .OrderBy(x => x.CreatedAt)
            .ToList();

        var result = new List<StudyPlanDto>();

        foreach (var p in ordered)
        {
            var problemCount = await _repo.GetItemCountAsync(p.Id);

            result.Add(new StudyPlanDto
            {
                Id = p.Id,
                Title = p.Title,
                Order = 0,
                Price = p.Price,      // ✅ thêm
                IsPaid = p.IsPaid,    // ✅ thêm
                ProblemCount = problemCount,
                IsCompleted = false,
                IsUnlocked = !p.IsPaid // tạm basic rule: free = unlocked
            });
        }

        return result;
    }
}