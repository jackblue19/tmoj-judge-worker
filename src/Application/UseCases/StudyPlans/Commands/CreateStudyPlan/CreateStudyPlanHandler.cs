using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Commands.CreateStudyPlan;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class CreateStudyPlanHandler : IRequestHandler<CreateStudyPlanCommand, Guid>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<CreateStudyPlanHandler> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public CreateStudyPlanHandler(
        IStudyPlanRepository repo,
        ILogger<CreateStudyPlanHandler> logger,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _logger = logger;
        _httpContext = httpContext;
    }

    public async Task<Guid> Handle(CreateStudyPlanCommand request, CancellationToken ct)
    {
        var userIdStr = _httpContext.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = Guid.Parse(userIdStr);

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

        await _repo.CreateAsync(plan);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ StudyPlan created: {Id}", plan.Id);

        return plan.Id;
    }
}