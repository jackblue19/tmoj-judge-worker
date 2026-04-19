using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanEnrollment;

public class GetStudyPlanEnrollmentHandler
    : IRequestHandler<GetStudyPlanEnrollmentQuery, StudyPlanEnrollmentDto>
{
    private readonly IStudyPlanRepository _repo;

    public GetStudyPlanEnrollmentHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<StudyPlanEnrollmentDto> Handle(
        GetStudyPlanEnrollmentQuery request,
        CancellationToken ct)
    {
        // 1. Get plan items
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);

        var totalItems = items.Count;

        if (totalItems == 0)
        {
            return new StudyPlanEnrollmentDto
            {
                StudyPlanId = request.StudyPlanId,
                UserId = request.UserId,
                IsEnrolled = false,
                IsCompleted = false,
                TotalItems = 0,
                CompletedItems = 0,
                ProgressPercent = 0
            };
        }

        // 2. Get progress (ONLY 1 QUERY)
        var progresses = await _repo.GetItemProgressByPlanAsync(
            request.UserId,
            request.StudyPlanId
        );

        var completedSet = progresses
            .Where(x => x.IsCompleted == true)
            .Select(x => x.StudyPlanItemId)
            .ToHashSet();

        var completedCount = completedSet.Count;

        // 3. Enrolled = có bất kỳ progress nào
        var isEnrolled = progresses.Any();

        // 4. Completed = full items done
        var isCompleted = completedCount == totalItems && totalItems > 0;

        // 5. Progress %
        var percent = totalItems == 0
            ? 0
            : (double)completedCount / totalItems * 100;

        return new StudyPlanEnrollmentDto
        {
            StudyPlanId = request.StudyPlanId,
            UserId = request.UserId,
            IsEnrolled = isEnrolled,
            IsCompleted = isCompleted,
            TotalItems = totalItems,
            CompletedItems = completedCount,
            ProgressPercent = percent
        };
    }
}