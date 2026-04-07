using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Commands;

public class RejectReportCommandHandler : IRequestHandler<RejectReportCommand, Unit>
{
    private readonly IReadRepository<ContentReport, Guid> _readRepo;
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public RejectReportCommandHandler(
        IReadRepository<ContentReport, Guid> readRepo,
        IWriteRepository<ContentReport, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task<Unit> Handle(RejectReportCommand request, CancellationToken ct)
    {
        var report = await _readRepo.GetByIdAsync(request.ReportId, ct)
            ?? throw new Exception("Report not found");

        if (!string.Equals(report.Status, "pending", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Already processed");

        report.Status = "rejected";

        _writeRepo.Update(report);
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}