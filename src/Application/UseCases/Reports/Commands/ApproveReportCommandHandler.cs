using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Commands;

namespace Application.UseCases.Reports.Commands;

public class ApproveReportCommandHandler
    : IRequestHandler<ApproveReportCommand, Unit>
{
    private readonly IReadRepository<ContentReport, Guid> _readRepo;
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public ApproveReportCommandHandler(
        IReadRepository<ContentReport, Guid> readRepo,
        IWriteRepository<ContentReport, Guid> writeRepo,
        IMediator mediator,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _mediator = mediator;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ApproveReportCommand request, CancellationToken ct)
    {
        var report = await _readRepo.GetByIdAsync(request.ReportId, ct)
            ?? throw new Exception("Report not found");

        if (report.Status != "Pending")
            throw new Exception("Already processed");

        // update report
        report.Status = "Approved";
        _writeRepo.Update(report);

        // 🔥 dùng lại command hide có sẵn
        await _mediator.Send(
            new HideUnhideCommentCommand(report.TargetId, true), ct);

        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}