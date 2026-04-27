using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.StudyPlans.Commands.RemoveProblemFromPlan;

public class RemoveProblemFromPlanHandler : IRequestHandler<RemoveProblemFromPlanCommand, bool>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<RemoveProblemFromPlanHandler> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public RemoveProblemFromPlanHandler(
        IStudyPlanRepository repo,
        ILogger<RemoveProblemFromPlanHandler> logger,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _logger = logger;
        _httpContext = httpContext;
    }

    public async Task<bool> Handle(RemoveProblemFromPlanCommand request, CancellationToken ct)
    {
        var userIdStr = _httpContext.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = Guid.Parse(userIdStr);

        var plan = await _repo.GetByIdAsync(request.StudyPlanId);
        if (plan == null)
            throw new Exception("Study plan not found");

        var isAdmin = _httpContext.HttpContext?.User?.IsInRole("admin") == true;
        if (plan.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("You are not allowed to modify this study plan");

        var itemToRemove = plan.StudyPlanItems.FirstOrDefault(x => x.ProblemId == request.ProblemId);
        if (itemToRemove == null)
            throw new Exception("Problem is not in this study plan");

        // 1. Delete associated UserStudyItemProgress (cleanup)
        // Unfortunately, plan.StudyPlanItems doesn't eagerly load UserStudyItemProgresses
        // Let's get them from repo
        var allProgress = await _repo.GetAllProgressByPlanAsync(request.StudyPlanId);
        var progressToRemove = allProgress.Where(x => x.StudyPlanItemId == itemToRemove.Id).ToList();
        
        if (progressToRemove.Any())
        {
            await _repo.DeleteItemProgressRangeAsync(progressToRemove);
        }

        // 2. Remove the StudyPlanItem
        _repo.RemoveItem(itemToRemove);
        
        // 3. Save
        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ Problem {ProblemId} removed from StudyPlan {PlanId}", request.ProblemId, request.StudyPlanId);

        return true;
    }
}
