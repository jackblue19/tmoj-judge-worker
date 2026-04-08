using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.Reports.Dtos;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class ContentReportRepository : IContentReportRepository
    {
        private readonly TmojDbContext _db;

        public ContentReportRepository(TmojDbContext db)
        {
            _db = db;
        }

        public async Task<CursorPaginationDto<ReportDto>> GetPendingReportsAsync(
      DateTime? cursorCreatedAt,
      Guid? cursorId,
      int pageSize)
        {
            // 🔥 1. query base (KHÔNG order ở đây)
            var query = _db.ContentReports
           .AsNoTracking()
           .Where(x => EF.Functions.ILike(x.Status, "pending"));

            // 🔥 2. cursor filter
            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt < cursorCreatedAt ||
                    (x.CreatedAt == cursorCreatedAt &&
                     x.Id.CompareTo(cursorId.Value) < 0));
            }

            // 🔥 3. order riêng
            var orderedQuery = query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id);

            // 🔥 4. query data
            var list = await orderedQuery
                .Take(pageSize + 1)
                .Select(x => new ReportDto
                {
                    Id = x.Id,
                    TargetId = x.TargetId,
                    TargetType = x.TargetType,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            var hasMore = list.Count > pageSize;
            if (hasMore)
                list.RemoveAt(pageSize);

            var last = list.LastOrDefault();

            return new CursorPaginationDto<ReportDto>
            {
                Items = list,
                NextCursorCreatedAt = last?.CreatedAt,
                NextCursorId = last?.Id,
                HasMore = hasMore
            };
        }

        public async Task<ReportDto?> GetByIdAsync(Guid id)
        {
            return await _db.ContentReports
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ReportDto
                {
                    Id = x.Id,
                    TargetId = x.TargetId,
                    TargetType = x.TargetType,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();
        }
        public async Task<List<ReportGroupDto>> GetReportGroupsAsync(string? status)
        {
            var query = _db.ContentReports.AsNoTracking();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(x => x.Status.ToLower() == status.ToLower());
            }

            var grouped = await query
                .GroupBy(x => new { x.TargetId, x.TargetType })
                .Select(g => new ReportGroupDto
                {
                    TargetId = g.Key.TargetId,
                    TargetType = g.Key.TargetType,

                    TotalReports = g.Count(),
                    PendingCount = g.Count(x => x.Status == "pending"),
                    ApprovedCount = g.Count(x => x.Status == "approved"),

                    LatestCreatedAt = g.Max(x => x.CreatedAt),

                    Reasons = g.Select(x => x.Reason).Distinct().ToList()
                })
                .OrderByDescending(x => x.LatestCreatedAt)
                .ToListAsync();

            return grouped;
        }
    }
}