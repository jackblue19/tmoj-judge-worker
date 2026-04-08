using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.Reports.Specs;
using Application.UseCases.DiscussionComments.Commands;

namespace Application.UseCases.Reports.Commands;

public class RejectReportCommandHandler : IRequestHandler<RejectReportCommand, Unit>
{
    private readonly IReadRepository<ContentReport, Guid> _readRepo;
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;

    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IReadRepository<User, Guid> _userRepo;

    private readonly IWriteRepository<ModerationAction, Guid> _actionRepo;

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public RejectReportCommandHandler(
        IReadRepository<ContentReport, Guid> readRepo,
        IWriteRepository<ContentReport, Guid> writeRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
        IReadRepository<User, Guid> userRepo,
        IWriteRepository<ModerationAction, Guid> actionRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _commentRepo = commentRepo;
        _discussionRepo = discussionRepo;
        _userRepo = userRepo;
        _actionRepo = actionRepo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(RejectReportCommand request, CancellationToken ct)
    {
        var report = await _readRepo.GetByIdAsync(request.ReportId, ct)
            ?? throw new Exception("Report not found");

        if (!string.Equals(report.Status, "pending", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Already processed");

        var adminId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        // 🔥 1. Reject report
        report.Status = "rejected";
        _writeRepo.Update(report);

        Guid? targetUserId = null;

        // =====================================================
        // 🔥 HANDLE COMMENT
        // =====================================================
        if (string.Equals(report.TargetType, "comment", StringComparison.OrdinalIgnoreCase))
        {
            var comment = await _commentRepo.GetByIdAsync(report.TargetId, ct)
                ?? throw new Exception("Comment not found");

            targetUserId = comment.UserId;

            // 🔥 COUNT pending reports
            var pendingCount = await _readRepo.CountAsync(
                new ReportsByTargetAndStatusSpec(report.TargetId, "comment", "pending"), ct);

            // 🔥 UNHIDE nếu <3
            if (pendingCount < 3)
            {
                await _mediator.Send(
                    new HideUnhideCommentCommand(report.TargetId, false), ct);
            }
        }

        // =====================================================
        // 🔥 HANDLE DISCUSSION
        // =====================================================
        else if (string.Equals(report.TargetType, "discussion", StringComparison.OrdinalIgnoreCase))
        {
            var discussion = await _discussionRepo.GetByIdAsync(report.TargetId, ct)
                ?? throw new Exception("Discussion not found");

            targetUserId = discussion.UserId;

            // 🔥 COUNT pending reports
            var pendingCount = await _readRepo.CountAsync(
                new ReportsByTargetAndStatusSpec(report.TargetId, "discussion", "pending"), ct);

            // 🔥 UNLOCK nếu <3
            if (pendingCount < 3)
            {
                discussion.IsLocked = false;
            }
        }

        // =====================================================
        // 🔥 AUTO UNBAN
        // =====================================================
        if (targetUserId.HasValue)
        {
            var approvedCount = await _readRepo.CountAsync(
                new ReportsByTargetAndStatusSpec(
                    report.TargetId,
                    report.TargetType,
                    "approved"),
                ct);

            if (approvedCount < 5)
            {
                var user = await _userRepo.GetByIdAsync(targetUserId.Value, ct);

                if (user != null && user.Status == false)
                {
                    user.Status = true;

                    await _actionRepo.AddAsync(new ModerationAction
                    {
                        Id = Guid.NewGuid(),
                        ReportId = Guid.Empty,
                        AdminId = adminId,
                        ActionType = "unban_user",
                        Note = $"Auto unbanned (approved reports: {approvedCount})",
                        CreatedAt = now
                    }, ct);
                }
            }
        }

        // =====================================================
        // 🔥 LOG REJECT
        // =====================================================
        await _actionRepo.AddAsync(new ModerationAction
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            AdminId = adminId,
            ActionType = "reject",
            Note = $"Rejected {report.TargetType} report",
            CreatedAt = now
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}