using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.StudyPlans.Commands.EnrollStudyPlan;

public class EnrollStudyPlanHandler
    : IRequestHandler<EnrollStudyPlanCommand, Unit>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<EnrollStudyPlanHandler> _logger;

    public EnrollStudyPlanHandler(
        IStudyPlanRepository repo,
        ILogger<EnrollStudyPlanHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Unit> Handle(EnrollStudyPlanCommand request, CancellationToken ct)
    {
        _logger.LogInformation("🚀 Enroll START: {@Request}", request);

        var plan = await _repo.GetByIdAsync(request.StudyPlanId);

        if (plan == null)
            throw new Exception("Study plan not found");

        // =========================
        // CHECK PAID
        // =========================
        if (plan.IsPaid)
        {
            _logger.LogWarning("💰 Plan is paid");

            // TODO: check purchase sau
            throw new Exception("This plan requires purchase");
        }

        // =========================
        // CHECK DUPLICATE
        // =========================
        var isEnrolled = await _repo.IsUserEnrolledAsync(
            request.UserId,
            request.StudyPlanId
        );

        if (isEnrolled)
        {
            _logger.LogWarning("⚠️ Already enrolled");
            return Unit.Value;
        }

        // =========================
        // INIT PROGRESS (QUAN TRỌNG)
        // =========================
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);

        foreach (var item in items)
        {
            await _repo.CreateItemProgressAsync(new UserStudyItemProgress
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                StudyPlanItemId = item.Id,
                IsCompleted = false,
                CompletedAt = null
            });
        }

        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ Enroll SUCCESS");

        return Unit.Value;
    }
}