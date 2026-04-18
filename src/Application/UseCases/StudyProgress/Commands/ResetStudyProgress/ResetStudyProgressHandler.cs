using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.StudyProgress.Commands.ResetStudyProgress;

public class ResetStudyProgressHandler
    : IRequestHandler<ResetStudyProgressCommand, Unit>
{
    private readonly IStudyPlanRepository _repo;

    public ResetStudyProgressHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<Unit> Handle(ResetStudyProgressCommand request, CancellationToken ct)
    {
        // 1. Get all items in plan
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);

        if (items.Count == 0)
            return Unit.Value;

        var itemIds = items.Select(x => x.Id).ToList();

        // 2. Get ALL progress of user (repo abstraction)
        var progresses = await _repo.GetAllItemProgressByUserAsync(request.UserId);

        // 3. Filter only this plan
        var toDelete = progresses
            .Where(x => itemIds.Contains(x.StudyPlanItemId))
            .ToList();

        if (toDelete.Count == 0)
            return Unit.Value;

        // 4. Delete via repository (no DbContext here)
        await _repo.DeleteItemProgressRangeAsync(toDelete);

        await _repo.SaveChangesAsync();

        return Unit.Value;
    }
}