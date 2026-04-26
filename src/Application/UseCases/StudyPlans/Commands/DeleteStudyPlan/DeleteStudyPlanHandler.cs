using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.StudyPlans.Commands.DeleteStudyPlan;

public class DeleteStudyPlanHandler : IRequestHandler<DeleteStudyPlanCommand, bool>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<DeleteStudyPlanHandler> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public DeleteStudyPlanHandler(
        IStudyPlanRepository repo,
        ILogger<DeleteStudyPlanHandler> logger,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _logger = logger;
        _httpContext = httpContext;
    }

    public async Task<bool> Handle(DeleteStudyPlanCommand request, CancellationToken ct)
    {
        var userIdStr = _httpContext.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = Guid.Parse(userIdStr);

        var plan = await _repo.GetByIdAsync(request.StudyPlanId);
        if (plan == null)
            throw new Exception("Study plan not found");

        // Verify ownership or admin role
        var isAdmin = _httpContext.HttpContext?.User?.IsInRole("admin") == true;
        if (plan.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("You are not allowed to delete this study plan");

        // For now, doing a soft delete by setting IsPublic to false or hard delete.
        // Assuming IStudyPlanRepository has a Delete method, let's just do hard delete for now.
        _repo.Delete(plan);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ StudyPlan deleted: {Id}", plan.Id);

        return true;
    }
}
