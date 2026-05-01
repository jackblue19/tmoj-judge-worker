using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlans;

public class GetStudyPlansHandler
    : IRequestHandler<GetStudyPlansQuery, List<StudyPlanDto>>
{
    private readonly IStudyPlanRepository _repo;
    private readonly IHttpContextAccessor _httpContext;

    public GetStudyPlansHandler(
        IStudyPlanRepository repo,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _httpContext = httpContext;
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

        var userIdStr = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = string.IsNullOrEmpty(userIdStr) ? null : Guid.Parse(userIdStr);

        foreach (var p in ordered)
        {
            var problemCount = await _repo.GetItemCountAsync(p.Id);
            
            bool isUnlocked = !p.IsPaid;
            if (p.IsPaid && userId.HasValue)
            {
                isUnlocked = await _repo.HasUserPurchasedPlanAsync(userId.Value, p.Id);
            }

            result.Add(new StudyPlanDto
            {
                Id = p.Id,
                Title = p.Title,
                Order = 0,
                Price = p.Price,      // ✅ thêm
                IsPaid = p.IsPaid,    // ✅ thêm
                ProblemCount = problemCount,
                IsCompleted = false,
                IsUnlocked = isUnlocked,
                ImageUrl = p.ImageUrl,
                EnrollmentCount = p.EnrollmentCount
            });
        }

        return result;
    }
}