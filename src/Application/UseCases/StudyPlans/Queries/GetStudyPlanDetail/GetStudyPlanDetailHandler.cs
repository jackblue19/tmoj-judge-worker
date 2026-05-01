using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanDetail;

public class GetStudyPlanDetailHandler
    : IRequestHandler<GetStudyPlanDetailQuery, StudyPlanDetailDto>
{
    private readonly IStudyPlanRepository _repo;
    private readonly IHttpContextAccessor _httpContext;

    public GetStudyPlanDetailHandler(
        IStudyPlanRepository repo,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _httpContext = httpContext;
    }

    public async Task<StudyPlanDetailDto> Handle(
        GetStudyPlanDetailQuery request,
        CancellationToken ct)
    {
        var plan = await _repo.GetByIdAsync(request.StudyPlanId);

        if (plan == null)
            throw new Exception("StudyPlan not found");

        var userIdStr = _httpContext.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            throw new UnauthorizedAccessException();

        var userId = Guid.Parse(userIdStr);

        var items = await _repo.GetItemsByPlanIdAsync(plan.Id);

        var ordered = items
            .OrderBy(x => x.OrderIndex)
            .ToList();

        var hasAccess =
            !plan.IsPaid ||
            await _repo.HasUserPurchasedPlanAsync(userId, plan.Id);

        var progresses = await _repo.GetItemProgressByPlanAsync(userId, plan.Id);

        var completedSet = progresses
            .Where(x => x.IsCompleted == true)
            .Select(x => x.StudyPlanItemId)
            .ToHashSet();

        var resultItems = new List<StudyPlanItemDto>();

        for (int i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];

            var prevCompleted =
                i == 0 ||
                completedSet.Contains(ordered[i - 1].Id);

            resultItems.Add(new StudyPlanItemDto
            {
                StudyPlanItemId = item.Id,
                ProblemId = item.ProblemId,
                ProblemTitle = item.Problem.Title,
                Order = item.OrderIndex,

                // 🔥 FIX NULLABLE
                IsCompleted = completedSet.Contains(item.Id),

                IsUnlocked = hasAccess && prevCompleted
            });
        }

        return new StudyPlanDetailDto
        {
            Id = plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            Price = plan.Price,
            IsPaid = plan.IsPaid,
            ImageUrl = plan.ImageUrl,
            EnrollmentCount = plan.EnrollmentCount,
            Items = resultItems
        };
    }
}