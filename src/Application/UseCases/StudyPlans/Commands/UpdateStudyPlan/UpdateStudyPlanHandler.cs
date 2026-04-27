using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.StudyPlans.Commands.UpdateStudyPlan;

public class UpdateStudyPlanHandler : IRequestHandler<UpdateStudyPlanCommand, bool>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<UpdateStudyPlanHandler> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public UpdateStudyPlanHandler(
        IStudyPlanRepository repo,
        ILogger<UpdateStudyPlanHandler> logger,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _logger = logger;
        _httpContext = httpContext;
    }

    public async Task<bool> Handle(UpdateStudyPlanCommand request, CancellationToken ct)
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
            throw new UnauthorizedAccessException("You are not allowed to update this study plan");

        // Business Rules
        if (!request.IsPaid)
        {
            request.Price = 0;
        }
        else if (request.Price <= 0)
        {
            throw new Exception("Paid plan must have price > 0");
        }

        plan.Title = request.Title;
        plan.Description = request.Description;
        plan.IsPublic = request.IsPublic;
        plan.IsPaid = request.IsPaid;
        plan.Price = request.Price;
        plan.ImageUrl = request.ImageUrl;

        _repo.Update(plan);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ StudyPlan updated: {Id}", plan.Id);

        return true;
    }
}
