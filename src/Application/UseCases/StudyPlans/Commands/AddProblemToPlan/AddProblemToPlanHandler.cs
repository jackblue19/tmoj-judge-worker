using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.StudyPlans.Commands.AddProblemToPlan;

public class AddProblemToPlanHandler : IRequestHandler<AddProblemToPlanCommand, Unit>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<AddProblemToPlanHandler> _logger;

    public AddProblemToPlanHandler(
        IStudyPlanRepository repo,
        ILogger<AddProblemToPlanHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Unit> Handle(AddProblemToPlanCommand request, CancellationToken ct)
    {
        _logger.LogInformation("🚀 START AddProblemToPlan");
        _logger.LogInformation("📌 StudyPlanId = {PlanId}", request.StudyPlanId);
        _logger.LogInformation("📌 ProblemId = {ProblemId}", request.ProblemId);

        // =========================
        // CHECK PLAN EXISTS
        // =========================
        var plan = await _repo.GetByIdAsync(request.StudyPlanId);

        if (plan == null)
        {
            _logger.LogError("❌ StudyPlan NOT FOUND: {PlanId}", request.StudyPlanId);
            throw new Exception("StudyPlan not found");
        }

        // =========================
        // LOAD ITEMS
        // =========================
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);

        _logger.LogInformation("📦 Current items count: {Count}", items.Count);

        // =========================
        // CHECK DUPLICATE
        // =========================
        if (items.Any(x => x.ProblemId == request.ProblemId))
        {
            _logger.LogWarning("⚠️ DUPLICATE problem in plan");
            throw new Exception("Problem already exists in study plan");
        }

        // =========================
        // ORDER INDEX
        // =========================
        var nextOrder = items.Count == 0
            ? 1
            : items.Max(x => x.OrderIndex) + 1;

        _logger.LogInformation("📌 NextOrder = {Order}", nextOrder);

        // =========================
        // CREATE ENTITY
        // =========================
        var entity = new StudyPlanItem
        {
            Id = Guid.NewGuid(),
            StudyPlanId = request.StudyPlanId,
            ProblemId = request.ProblemId,
            OrderIndex = nextOrder
        };

        await _repo.AddItemAsync(entity);

        _logger.LogInformation("💾 BEFORE SAVE");

        await _repo.SaveChangesAsync();

        _logger.LogInformation("✅ AFTER SAVE SUCCESS");

        return Unit.Value;
    }
}