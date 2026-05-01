using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.StudyProgress.Commands.CompleteProblem;

public class CompleteProblemHandler : IRequestHandler<CompleteProblemCommand, Unit>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<CompleteProblemHandler> _logger;

    public CompleteProblemHandler(
        IStudyPlanRepository repo,
        ILogger<CompleteProblemHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Unit> Handle(CompleteProblemCommand request, CancellationToken ct)
    {
        _logger.LogInformation("🔥 CompleteProblem START: {@Request}", request);

        var progress = await _repo.GetItemProgressAsync(
            request.UserId,
            request.StudyPlanItemId
        );

        if (progress == null)
        {
            _logger.LogInformation("➕ Creating new progress");

            await _repo.CreateItemProgressAsync(new UserStudyItemProgress
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                StudyPlanItemId = request.StudyPlanItemId,
                IsCompleted = true,
                CompletedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            });
        }
        else
        {
            if (progress.IsCompleted == true)
            {
                _logger.LogWarning("⚠️ Already completed");
                return Unit.Value;
            }

            _logger.LogInformation("🔄 Updating progress");

            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        }

        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ CompleteProblem SUCCESS");

        return Unit.Value;
    }
}