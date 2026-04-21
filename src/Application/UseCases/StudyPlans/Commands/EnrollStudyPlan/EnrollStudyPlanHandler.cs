using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Application.UseCases.StudyPlans.Commands.EnrollStudyPlan;

public class EnrollStudyPlanHandler
    : IRequestHandler<EnrollStudyPlanCommand, Unit>
{
    private readonly IStudyPlanRepository _repo;
    private readonly IHttpContextAccessor _httpContext;

    public EnrollStudyPlanHandler(
        IStudyPlanRepository repo,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _httpContext = httpContext;
    }

    public async Task<Unit> Handle(EnrollStudyPlanCommand request, CancellationToken ct)
    {
        // =========================
        // GET USER
        // =========================
        var userIdStr = _httpContext.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            throw new UnauthorizedAccessException();

        var userId = Guid.Parse(userIdStr);

        // =========================
        // CHECK ALREADY ENROLLED
        // =========================
        var isEnrolled = await _repo.IsUserEnrolledAsync(userId, request.StudyPlanId);

        if (isEnrolled)
            return Unit.Value; // idempotent

        // =========================
        // GET ITEMS
        // =========================
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);

        if (items.Count == 0)
            return Unit.Value;

        // =========================
        // CREATE PROGRESS FOR ALL ITEMS
        // =========================
        var progresses = items.Select(i => new UserStudyItemProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StudyPlanItemId = i.Id,
            IsCompleted = false,
            CompletedAt = null
        }).ToList();

        foreach (var p in progresses)
        {
            await _repo.CreateItemProgressAsync(p);
        }

        await _repo.SaveChangesAsync();

        return Unit.Value;
    }
}