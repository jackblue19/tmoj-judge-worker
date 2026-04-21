using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using Application.UseCases.StudyPlans.Queries.GetStudyPlanEnrollment;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class GetStudyPlanEnrollmentHandler
    : IRequestHandler<GetStudyPlanEnrollmentQuery, StudyPlanEnrollmentDto>
{
    private readonly IStudyPlanRepository _repo;
    private readonly IHttpContextAccessor _httpContext;

    public GetStudyPlanEnrollmentHandler(
        IStudyPlanRepository repo,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _httpContext = httpContext;
    }

    public async Task<StudyPlanEnrollmentDto> Handle(
        GetStudyPlanEnrollmentQuery request,
        CancellationToken ct)
    {
        // =========================
        // GET USER FROM TOKEN
        // =========================
        var userIdStr = _httpContext.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            throw new UnauthorizedAccessException();

        var userId = Guid.Parse(userIdStr);

        // =========================
        // GET ITEMS
        // =========================
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);
        var totalItems = items.Count;

        if (totalItems == 0)
        {
            return new StudyPlanEnrollmentDto
            {
                StudyPlanId = request.StudyPlanId,
                UserId = userId,
                IsEnrolled = false,
                IsCompleted = false,
                TotalItems = 0,
                CompletedItems = 0,
                ProgressPercent = 0
            };
        }

        // =========================
        // GET PROGRESS (1 QUERY)
        // =========================
        var progresses = await _repo.GetItemProgressByPlanAsync(
            userId,
            request.StudyPlanId
        );

        var completedSet = progresses
            .Where(x => x.IsCompleted == true)
            .Select(x => x.StudyPlanItemId)
            .ToHashSet();

        var completedCount = completedSet.Count;

        var isEnrolled = progresses.Any();

        var isCompleted = completedCount == totalItems;

        var percent = (double)completedCount / totalItems * 100;

        return new StudyPlanEnrollmentDto
        {
            StudyPlanId = request.StudyPlanId,
            UserId = userId,
            IsEnrolled = isEnrolled,
            IsCompleted = isCompleted,
            TotalItems = totalItems,
            CompletedItems = completedCount,
            ProgressPercent = percent
        };
    }
}