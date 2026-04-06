using MediatR;
using Application.Common.Pagination;
using Application.UseCases.Reports.Dtos;

namespace Application.UseCases.Reports.Queries;

public record GetPendingReportsQuery(
    DateTime? CursorCreatedAt,
    Guid? CursorId,
    int PageSize
) : IRequest<CursorPaginationDto<ReportDto>>;