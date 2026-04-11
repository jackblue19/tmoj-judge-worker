using Application.Common.Pagination;
using Application.UseCases.Reports.Dtos;
using System;

namespace Application.Common.Interfaces
{
    public interface IContentReportRepository
    {
        Task<CursorPaginationDto<ReportDto>> GetPendingReportsAsync(
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize);

        Task<ReportDto?> GetByIdAsync(Guid id);

        Task<List<ReportGroupDto>> GetReportGroupsAsync(string? status);

        // 🔥 NEW
        Task<(Guid? AuthorId, string? AuthorName)> GetAuthorInfoAsync(Guid targetId, string targetType);
    }
}