using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.StudyPlans.Commands.EnrollStudyPlan;

public class EnrollStudyPlanHandler : IRequestHandler<EnrollStudyPlanCommand, Unit>
{
    private readonly IStudyPlanRepository _repo;
    private readonly IUserStudyPlanPurchaseRepository _purchaseRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationRepository _notificationRepo;
    private readonly ILogger<EnrollStudyPlanHandler> _logger;

    public EnrollStudyPlanHandler(
        IStudyPlanRepository repo,
        IUserStudyPlanPurchaseRepository purchaseRepo,
        ICurrentUserService currentUser,
        INotificationRepository notificationRepo,
        ILogger<EnrollStudyPlanHandler> logger)
    {
        _repo = repo;
        _purchaseRepo = purchaseRepo;
        _currentUser = currentUser;
        _notificationRepo = notificationRepo;
        _logger = logger;
    }

    public async Task<Unit> Handle(EnrollStudyPlanCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        _logger.LogInformation("ENROLL START | User={UserId} Plan={PlanId}", userId, request.StudyPlanId);

        var plan = await _repo.GetByIdAsync(request.StudyPlanId)
            ?? throw new Exception("StudyPlan not found");

        var isFree = !plan.IsPaid;
        var hasAccess = await _purchaseRepo.ExistsAsync(userId, plan.Id);

        _logger.LogInformation("PLAN INFO | IsFree={IsFree} HasAccess={HasAccess}", isFree, hasAccess);

        if (!isFree && !hasAccess)
            throw new Exception("You must buy this plan first");

        var isEnrolled = await _repo.IsUserEnrolledAsync(userId, plan.Id);

        if (isEnrolled)
        {
            _logger.LogInformation("Already enrolled | User={UserId} Plan={PlanId}", userId, plan.Id);
            return Unit.Value;
        }

        var items = await _repo.GetItemsByPlanIdAsync(plan.Id);

        _logger.LogInformation("PLAN ITEMS COUNT = {Count}", items.Count);

        // =========================
        // FIX CRITICAL: EMPTY PLAN
        // =========================
        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("ENROLL FAILED: StudyPlan has no items");
            throw new Exception("StudyPlan has no items. Add problems before enrolling.");
        }

        var progresses = new List<UserStudyItemProgress>();

        foreach (var item in items)
        {
            progresses.Add(new UserStudyItemProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StudyPlanItemId = item.Id,
                IsCompleted = false
            });
        }

        foreach (var p in progresses)
        {
            await _repo.CreateItemProgressAsync(p);
        }

        await _repo.SaveChangesAsync();
        
        // =========================
        // UPDATE ENROLLMENT COUNT
        // Tăng số lượng người tham gia (tự động)
        if (plan.EnrollmentCount == null) plan.EnrollmentCount = 0;
        plan.EnrollmentCount++;
        _repo.Update(plan);
        await _repo.SaveChangesAsync();

        // =========================
        // SEND NOTIFICATION
        // =========================
        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = "Đã tham gia khóa học",
            Message = $"Chúc mừng! Bạn đã tham gia thành công khóa học '{plan.Title}'. Hãy bắt đầu học ngay nhé!",
            Type = "system",
            ScopeType = "study_plan",
            ScopeId = plan.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notificationRepo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ENROLL SUCCESS | User={UserId} Plan={PlanId} Items={Count}",
            userId,
            plan.Id,
            progresses.Count
        );

        return Unit.Value;
    }
}