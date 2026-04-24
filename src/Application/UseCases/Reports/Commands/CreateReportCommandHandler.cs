using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.Reports.Specs;

namespace Application.UseCases.Reports.Commands;

public class CreateReportCommandHandler
    : IRequestHandler<CreateReportCommand, Guid>
{
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;
    private readonly IReadRepository<ContentReport, Guid> _reportRepo;
    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IWriteRepository<Notification, Guid> _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateReportCommandHandler(
        IWriteRepository<ContentReport, Guid> writeRepo,
        IReadRepository<ContentReport, Guid> reportRepo,
        IReadRepository<User, Guid> userRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
        IWriteRepository<Notification, Guid> notificationRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _writeRepo = writeRepo;
        _reportRepo = reportRepo;
        _userRepo = userRepo;
        _commentRepo = commentRepo;
        _discussionRepo = discussionRepo;
        _notificationRepo = notificationRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateReportCommand request, CancellationToken ct)
    {
        // 🔥 1. Validate
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required");

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated");

        // 🔥 2. Check user tồn tại
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user == null)
            throw new Exception("User not found");

        var targetType = request.TargetType?.ToLower();
        if (string.IsNullOrEmpty(targetType))
            throw new Exception("TargetType is required");

        Guid ownerId;

        // 🔥 3. HANDLE MULTI TARGET
        switch (targetType)
        {
            case "comment":
                var comment = await _commentRepo.GetByIdAsync(request.TargetId, ct);
                if (comment == null)
                    throw new Exception("Comment not found");

                ownerId = comment.UserId;

                if (ownerId == userId)
                    throw new Exception("Cannot report your own comment");

                break;

            case "discussion":
                var discussion = await _discussionRepo.GetByIdAsync(request.TargetId, ct);
                if (discussion == null)
                    throw new Exception("Discussion not found");

                ownerId = discussion.UserId;

                if (ownerId == userId)
                    throw new Exception("Cannot report your own discussion");

                break;

            default:
                throw new Exception($"Unsupported target type: {targetType}");
        }

        // 🔥 4. Anti duplicate
        var existed = await _reportRepo.FirstOrDefaultAsync(
            new ReportByUserAndTargetSpec(userId, request.TargetId, targetType), ct);

        if (existed != null)
            throw new Exception("You already reported this content");

        // 🔥 5. FIX DateTime (KHÔNG ĐỤNG DB)
        var createdAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        // 🔥 6. Create report
        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = userId,
            TargetId = request.TargetId,
            TargetType = targetType,
            Reason = request.Reason.Trim(),
            Status = "pending",
            CreatedAt = createdAt
        };

        await _writeRepo.AddAsync(report, ct);

        // 🔥 7. AUTO MODERATION (chỉ áp dụng cho comment)
        if (targetType == "comment")
        {
            var count = await _reportRepo.CountAsync(
                new ReportsByTargetAndStatusSpec(request.TargetId, "comment", "pending"), ct);

            if (count >= 2)
            {
                var comment = await _commentRepo.GetByIdAsync(request.TargetId, ct);
                if (comment != null && comment.IsHidden != true)
                {
                    comment.IsHidden = true;

                    await _notificationRepo.AddAsync(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = comment.UserId,
                        Title = "Bình luận bị tạm ẩn",
                        Message = "Bình luận của bạn đã bị cộng đồng báo cáo nhiều lần và đang bị hệ thống tạm ẩn để chờ Ban Quản Trị xem xét.",
                        Type = "system",
                        ScopeType = "comment",
                        ScopeId = request.TargetId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                }
            }
        }

        // 🔥 8. Save + debug lỗi thật
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.InnerException?.Message ?? ex.Message);
        }

        return report.Id;
    }
}