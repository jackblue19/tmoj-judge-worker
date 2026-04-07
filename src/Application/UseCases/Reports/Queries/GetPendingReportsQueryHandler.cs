using MediatR;
using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.Reports.Dtos;

namespace Application.UseCases.Reports.Queries;

public class GetPendingReportsQueryHandler
    : IRequestHandler<GetPendingReportsQuery, CursorPaginationDto<ReportDto>>
{
    private readonly IContentReportRepository _repo;

    public GetPendingReportsQueryHandler(IContentReportRepository repo)
    {
        _repo = repo;
    }

    public async Task<CursorPaginationDto<ReportDto>> Handle(
        GetPendingReportsQuery request,
        CancellationToken ct)
    {
        return await _repo.GetPendingReportsAsync(
            request.CursorCreatedAt,
            request.CursorId,
            request.PageSize);
    }
}