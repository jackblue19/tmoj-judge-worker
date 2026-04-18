using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Commands.AddStudyPlan;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.UseCases.StudyPlans.Commands.CreateStudyPlan;

public class AddStudyPlanHandler : IRequestHandler<AddStudyPlanCommand, Guid>
{
    private readonly IStudyPlanRepository _repo;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<AddStudyPlanHandler> _logger;

    public AddStudyPlanHandler(
        IStudyPlanRepository repo,
        IHttpContextAccessor httpContext,
        ILogger<AddStudyPlanHandler> logger)
    {
        _repo = repo;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task<Guid> Handle(AddStudyPlanCommand request, CancellationToken ct)
    {
        // =========================
        // GET USER FROM JWT
        // =========================
        var userIdStr =
            _httpContext.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = Guid.Parse(userIdStr);

        _logger.LogInformation("🚀 Create StudyPlan by user {UserId}", userId);

        // =========================
        // BUILD ENTITY
        // =========================
        var plan = new StudyPlan
        {
            Id = Guid.NewGuid(),
            CreatorId = userId,

            Title = request.Title,
            Description = request.Description,

            IsPublic = request.IsPublic,
            IsPaid = request.IsPaid,
            Price = request.Price,

            CreatedAt = DateTime.UtcNow
        };

        // =========================
        // SAVE
        // =========================
        var id = await _repo.CreateAsync(plan);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ StudyPlan created: {Id}", id);

        return id;
    }
}