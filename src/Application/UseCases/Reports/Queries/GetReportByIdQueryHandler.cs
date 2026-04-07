using Application.UseCases.Reports.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ReportDto>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;

    public GetReportByIdQueryHandler(IReadRepository<ContentReport, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<ReportDto> Handle(GetReportByIdQuery request, CancellationToken ct)
    {
        var report = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new Exception("Report not found");

        return new ReportDto
        {
            Id = report.Id,
            TargetId = report.TargetId,
            TargetType = report.TargetType,
            Reason = report.Reason,
            Status = report.Status,
            CreatedAt = report.CreatedAt
        };
    }
}