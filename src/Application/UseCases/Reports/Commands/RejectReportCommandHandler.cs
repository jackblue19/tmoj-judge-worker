using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Commands;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Commands;

public class RejectReportCommandHandler : IRequestHandler<RejectReportCommand, Unit>
{
    private readonly IReadRepository<ContentReport, Guid> _readRepo;
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;

    private readonly IReadRepository<ModerationAction, Guid> _actionReadRepo;
    private readonly IWriteRepository<ModerationAction, Guid> _actionRepo;

    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IProblemDiscussionRepository _discussionRepo;

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public RejectReportCommandHandler(
        IReadRepository<ContentReport, Guid> readRepo,
        IWriteRepository<ContentReport, Guid> writeRepo,
        IReadRepository<ModerationAction, Guid> actionReadRepo,
        IWriteRepository<ModerationAction, Guid> actionRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IProblemDiscussionRepository discussionRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _actionReadRepo = actionReadRepo;
        _actionRepo = actionRepo;
        _commentRepo = commentRepo;
        _discussionRepo = discussionRepo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(RejectReportCommand request, CancellationToken ct)
    {
        Console.WriteLine($"[REJECT] ReportId = {request.ReportId}");

        var report = await _readRepo.GetByIdAsync(request.ReportId, ct);

        if (report == null)
            throw new Exception("Report not found");

        if (!string.Equals(report.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[REJECT] Already processed: {report.Status}");
            return Unit.Value;
        }

        // ✅ tránh duplicate
        var spec = new ModerationActionByReportAndTypeSpec(report.Id, "reject");

        var existed = await _actionReadRepo.FirstOrDefaultAsync(spec, ct);

        if (existed != null)
            return Unit.Value;

        var adminId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var now = DateTime.UtcNow;

        // COMMENT
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

            await _mediator.Send(new HideUnhideCommentCommand(report.TargetId, false), ct);
        }
        // DISCUSSION
        else if (report.TargetType == "discussion")
        {
            var discussion = await _discussionRepo.GetByIdAsync(report.TargetId);

            if (discussion == null)
            {
                report.Status = "orphaned";
                _writeRepo.Update(report);
                await _uow.SaveChangesAsync(ct);
                return Unit.Value;
            }

            await _discussionRepo.UnlockAsync(report.TargetId);
        }

        report.Status = "rejected";
        _writeRepo.Update(report);

        await _actionRepo.AddAsync(new ModerationAction
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            AdminId = adminId,
            ActionType = "reject",
            Note = request.Reason ?? "Rejected",
            CreatedAt = now
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}