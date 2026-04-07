using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Commands;
using Application.UseCases.Reports.Specs;

namespace Application.UseCases.Reports.Commands;

public class ApproveReportCommandHandler : IRequestHandler<ApproveReportCommand, Unit>
{
    private readonly IReadRepository<ContentReport, Guid> _readRepo;
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;
    private readonly IWriteRepository<ModerationAction, Guid> _actionRepo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ApproveReportCommandHandler(
        IReadRepository<ContentReport, Guid> readRepo,
        IWriteRepository<ContentReport, Guid> writeRepo,
        IWriteRepository<ModerationAction, Guid> actionRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IReadRepository<User, Guid> userRepo,
        IUnitOfWork uow,
        IMediator mediator,
        ICurrentUserService currentUser)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _actionRepo = actionRepo;
        _commentRepo = commentRepo;
        _userRepo = userRepo;
        _uow = uow;
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ApproveReportCommand request, CancellationToken ct)
    {
        var report = await _readRepo.GetByIdAsync(request.ReportId, ct)
            ?? throw new Exception("Report not found");

        if (!string.Equals(report.Status, "pending", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Already processed");

        report.Status = "approved";
        _writeRepo.Update(report);

        var adminId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var comment = await _commentRepo.GetByIdAsync(report.TargetId, ct);

        if (comment != null)
        {
            // 🔥 COUNT approved reports
            var count = await _readRepo.CountAsync(
                new ReportsByTargetAndStatusSpec(report.TargetId, "comment", "approved"), ct);

            // 🔥 BAN user nếu >=5
            if (count >= 5)
            {
                var user = await _userRepo.GetByIdAsync(comment.UserId, ct);

                if (user != null && user.Status == true)
                {
                    user.Status = false;

                    await _actionRepo.AddAsync(new ModerationAction
                    {
                        Id = Guid.NewGuid(),
                        ReportId = report.Id,
                        AdminId = adminId,
                        ActionType = "ban_user",
                        Note = $"Auto banned (reports: {count})",
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                }
            }

            // 🔥 AUTO HIDE COMMENT
            await _mediator.Send(
                new HideUnhideCommentCommand(report.TargetId, true), ct);
        }

        // 🔥 LOG APPROVE
        await _actionRepo.AddAsync(new ModerationAction
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            AdminId = adminId,
            ActionType = "approve",
            Note = "Approved report",
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}