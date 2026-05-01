using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Commands;
using Application.UseCases.DiscussionComments.Specs;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Commands;

public class ApproveReportCommandHandler : IRequestHandler<ApproveReportCommand, Unit>
{
    private readonly IReadRepository<ContentReport, Guid> _readRepo;
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;

    private readonly IReadRepository<ModerationAction, Guid> _actionReadRepo;
    private readonly IWriteRepository<ModerationAction, Guid> _actionRepo;

    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IProblemDiscussionRepository _discussionRepo;

    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly IWriteRepository<User, Guid> _userWriteRepo;
    private readonly INotificationRepository _notificationRepo;

    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ApproveReportCommandHandler(
        IReadRepository<ContentReport, Guid> readRepo,
        IWriteRepository<ContentReport, Guid> writeRepo,
        IReadRepository<ModerationAction, Guid> actionReadRepo,
        IWriteRepository<ModerationAction, Guid> actionRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IProblemDiscussionRepository discussionRepo,
        IReadRepository<User, Guid> userRepo,
        IWriteRepository<User, Guid> userWriteRepo,
        INotificationRepository notificationRepo,
        IUnitOfWork uow,
        IMediator mediator,
        ICurrentUserService currentUser)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _actionReadRepo = actionReadRepo;
        _actionRepo = actionRepo;
        _commentRepo = commentRepo;
        _discussionRepo = discussionRepo;
        _userRepo = userRepo;
        _userWriteRepo = userWriteRepo;
        _notificationRepo = notificationRepo;
        _uow = uow;
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ApproveReportCommand request, CancellationToken ct)
    {
        var report = await _readRepo.GetByIdAsync(request.ReportId, ct);

        if (report == null)
            throw new Exception("Report not found");

        if (!string.Equals(report.Status, "pending", StringComparison.OrdinalIgnoreCase))
            return Unit.Value;

        var existed = await _actionReadRepo.FirstOrDefaultAsync(
            new ModerationActionByReportAndTypeSpec(report.Id, "approve"), ct);

        if (existed != null)
            return Unit.Value;

        var adminId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        Guid? authorId = null;

        // =========================
        // HANDLE TARGET
        // =========================
        if (report.TargetType == "comment")
        {
            var comment = await _commentRepo.GetByIdAsync(report.TargetId, ct);

            if (comment == null)
            {
                report.Status = "orphaned";
                _writeRepo.Update(report);
                await _uow.SaveChangesAsync(ct);
                return Unit.Value;
            }

            authorId = comment.UserId;

            await _mediator.Send(new HideUnhideCommentCommand(report.TargetId, true), ct);
        }
        else if (report.TargetType == "discussion")
        {
            var discussion = await _discussionRepo.GetEntityByIdAsync(report.TargetId);

            if (discussion == null)
            {
                report.Status = "orphaned";
                _writeRepo.Update(report);
                await _uow.SaveChangesAsync(ct);
                return Unit.Value;
            }

            authorId = discussion.UserId;

            await _discussionRepo.LockAsync(report.TargetId);
        }

        // =========================
        // NOTIFY AUTHOR OF MODERATION
        // =========================
        if (authorId.HasValue)
        {
            var existingApproved = await _readRepo.CountAsync(
                new ReportsByTargetAndStatusSpec(report.TargetId, report.TargetType, "approved"), ct);

            if (existingApproved == 0)
            {
                var targetName = report.TargetType == "comment" ? "Bình luận" : "Bài thảo luận";
                await _notificationRepo.AddAsync(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = authorId.Value,
                    Title = $"{targetName} bị gỡ do vi phạm",
                    Message = $"{targetName} của bạn đã bị quản trị viên gỡ bỏ vì vi phạm tiêu chuẩn cộng đồng. Lý do: {report.Reason}",
                    Type = "system",
                    ScopeType = report.TargetType,
                    ScopeId = report.TargetId,
                    IsRead = false,
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                    CreatedBy = adminId
                }, ct);
            }
        }

        // =========================
        // UPDATE REPORT
        // =========================
        report.Status = "approved";
        _writeRepo.Update(report);

        // =========================
        // CREATE MODERATION ACTION
        // =========================
        await _actionRepo.AddAsync(new ModerationAction
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            AdminId = adminId,
            ActionType = "approve",
            Note = request.Reason ?? "Approved",
            CreatedAt = now
        }, ct);

        // =========================
        // 🔥 AUTO BAN USER
        // =========================
        if (authorId.HasValue)
        {
            var approvedReports = await _readRepo.ListAsync(
                new AllReportsSpec("approved"), ct);

            var commentIds = approvedReports
                .Where(x => x.TargetType == "comment")
                .Select(x => x.TargetId)
                .ToList();

            var discussionIds = approvedReports
                .Where(x => x.TargetType == "discussion")
                .Select(x => x.TargetId)
                .ToList();

            int commentCount = 0;
            int discussionCount = 0;

            if (commentIds.Any())
            {
                var comments = await _commentRepo.ListAsync(
                    new CommentsByIdsSpec(commentIds.Distinct().ToList()), ct);

                var userCommentIds = comments.Where(x => x.UserId == authorId).Select(x => x.Id).ToHashSet();
                commentCount = commentIds.Count(id => userCommentIds.Contains(id));
            }

            if (discussionIds.Any())
            {
                var discussions = await _discussionRepo
                    .GetDiscussionEntitiesByIdsAsync(discussionIds.Distinct().ToList());

                var userDiscussionIds = discussions.Where(x => x.UserId == authorId).Select(x => x.Id).ToHashSet();
                discussionCount = discussionIds.Count(id => userDiscussionIds.Contains(id));
            }

            // Cộng thêm report hiện tại vì nó chưa được save vào DB với status = approved
            var total = commentCount + discussionCount + 1;

            if (total >= 5)
            {
                var user = await _userRepo.GetByIdAsync(authorId.Value, ct);

                if (user != null && user.Status == true)
                {
                    user.Status = false;
                    _userWriteRepo.Update(user);

                    await _actionRepo.AddAsync(new ModerationAction
                    {
                        Id = Guid.NewGuid(),
                        ReportId = report.Id,
                        AdminId = adminId,
                        ActionType = "auto_ban_user",
                        Note = $"Auto banned (total approved reports = {total})",
                        CreatedAt = now
                    }, ct);

                    await _notificationRepo.AddAsync(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = authorId.Value,
                        Title = "Tài khoản bị cấm (Banned)",
                        Message = $"Tài khoản của bạn đã bị khóa do có quá nhiều nội dung vi phạm tiêu chuẩn cộng đồng ({total} vi phạm).",
                        Type = "system",
                        IsRead = false,
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                        CreatedBy = adminId
                    }, ct);
                }
            }
        }

        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}