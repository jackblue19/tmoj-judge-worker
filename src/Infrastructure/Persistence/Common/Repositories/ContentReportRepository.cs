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

        // =========================
        // GET AUTHOR INFO (🔥 CORE)
        // =========================
        public async Task<(Guid? AuthorId, string? AuthorName)> GetAuthorInfoAsync(Guid targetId, string targetType)
        {
            // COMMENT
            if (targetType == "comment")
            {
                var data = await _db.DiscussionComments
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Where(x => x.Id == targetId)
                    .Select(x => new
                    {
                        x.UserId,
                        Name = x.User.DisplayName ?? x.User.Username
                    })
                    .FirstOrDefaultAsync();

                return (data?.UserId, data?.Name);
            }

            // DISCUSSION
            if (targetType == "discussion")
            {
                var data = await _db.ProblemDiscussions
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Where(x => x.Id == targetId)
                    .Select(x => new
                    {
                        x.UserId,
                        Name = x.User.DisplayName ?? x.User.Username
                    })
                    .FirstOrDefaultAsync();

                return (data?.UserId, data?.Name);
            }

            return (null, null);
        }

        // =========================
        // PENDING REPORTS
        // =========================
        public async Task<CursorPaginationDto<ReportDto>> GetPendingReportsAsync(
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize)
        {
            var query = _db.ContentReports
                .AsNoTracking()
                .Where(x => EF.Functions.ILike(x.Status, "pending"));

            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt < cursorCreatedAt ||
                    (x.CreatedAt == cursorCreatedAt &&
                     x.Id.CompareTo(cursorId.Value) < 0));
            }

            var orderedQuery = query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id);

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

        // =========================
        // GET BY ID
        // =========================
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

        // =========================
        // GROUP
        // =========================
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