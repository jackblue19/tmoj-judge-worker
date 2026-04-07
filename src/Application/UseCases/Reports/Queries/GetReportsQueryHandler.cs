using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Queries;

public class GetReportsQueryHandler
    : IRequestHandler<GetReportsQuery, List<ReportDto>>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;

    public GetReportsQueryHandler(IReadRepository<ContentReport, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<List<ReportDto>> Handle(GetReportsQuery request, CancellationToken ct)
    {
        var reports = await _repo.ListAsync(
            new AllReportsSpec(request.Status), ct);

        return reports.Select(x => new ReportDto
        {
            Id = x.Id,
            TargetId = x.TargetId,
            TargetType = x.TargetType,
            Reason = x.Reason,
            Status = x.Status,
            CreatedAt = x.CreatedAt
        }).ToList();
    }
}