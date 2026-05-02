using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.StudyProgress.Commands.CompleteStudyPlanItem;

public class CompleteStudyPlanItemHandler
    : IRequestHandler<CompleteStudyPlanItemCommand, Unit>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<CompleteStudyPlanItemHandler> _logger;

    public CompleteStudyPlanItemHandler(
        IStudyPlanRepository repo,
        ILogger<CompleteStudyPlanItemHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Unit> Handle(CompleteStudyPlanItemCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Complete item START: {ItemId}", request.StudyPlanItemId);

        var progress = await _repo.GetItemProgressAsync(
            request.UserId,
            request.StudyPlanItemId
        );

        if (progress == null)
            throw new Exception("User is not enrolled or progress not initialized");

        if (progress.IsCompleted == true)
            return Unit.Value; // idempotent

        progress.IsCompleted = true;
        progress.CompletedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        await _repo.SaveChangesAsync();

        _logger.LogInformation("Complete item SUCCESS: {ItemId}", request.StudyPlanItemId);

        return Unit.Value;
    }
}