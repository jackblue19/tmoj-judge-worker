using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.ProblemDiscussions.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class ProblemDiscussionRepository : IProblemDiscussionRepository
    {
        private readonly TmojDbContext _db;

        public ProblemDiscussionRepository(TmojDbContext db)
        {
            _db = db;
        }

        public async Task<CursorPaginationDto<DiscussionResponseDto>> GetPagedAsync(
            Guid problemId,
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize)
        {
            var query =
                from d in _db.ProblemDiscussions.AsNoTracking()
                join u in _db.Users on d.UserId equals u.UserId into gj
                from u in gj.DefaultIfEmpty()
                where d.ProblemId == problemId
                select new { d, u };

            query = query
                .OrderByDescending(x => x.d.IsPinned)
                .ThenByDescending(x => x.d.CreatedAt)
                .ThenByDescending(x => x.d.Id);

            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                query = query.Where(x =>
                    x.d.CreatedAt < cursorCreatedAt ||
                    (x.d.CreatedAt == cursorCreatedAt &&
                     x.d.Id.CompareTo(cursorId.Value) < 0));
            }

            var list = await query
                .Take(pageSize + 1)
                .Select(x => new DiscussionResponseDto
                {
                    Id = x.d.Id,
                    ProblemId = x.d.ProblemId,
                    UserId = x.d.UserId,

                    UserDisplayName =
                        x.u != null
                            ? (x.u.DisplayName ?? x.u.Username ?? "Anonymous")
                            : "Anonymous",

                    UserAvatarUrl = x.u != null ? x.u.AvatarUrl : null,

                    Title = x.d.Title,
                    Content = x.d.Content,
                    IsPinned = x.d.IsPinned ?? false,
                    IsLocked = x.d.IsLocked ?? false,
                    CreatedAt = x.d.CreatedAt
                })
                .ToListAsync();

            var hasMore = list.Count > pageSize;
            if (hasMore)
                list.RemoveAt(pageSize);

            var last = list.LastOrDefault();

            return new CursorPaginationDto<DiscussionResponseDto>
            {
                Items = list,
                NextCursorCreatedAt = last?.CreatedAt,
                NextCursorId = last?.Id,
                HasMore = hasMore
            };
        }
        public async Task<DiscussionResponseDto?> GetByIdAsync(Guid id)
        {
            return await _db.ProblemDiscussions
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.Id == id)
                .Select(x => new DiscussionResponseDto
                {
                    Id = x.Id,
                    ProblemId = x.ProblemId,
                    UserId = x.UserId,

                    // ✅ FIX NULL NAME
                    UserDisplayName = x.User.DisplayName
                        ?? x.User.Username
                        ?? "Anonymous",

                    UserAvatarUrl = x.User.AvatarUrl,

                    Title = x.Title,
                    Content = x.Content,
                    IsPinned = x.IsPinned ?? false,
                    IsLocked = x.IsLocked ?? false,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();
        }
        public async Task<DiscussionResponseDto?> GetByIdWithUserAsync(Guid id)
        {
            return await _db.ProblemDiscussions
                .AsNoTracking()
                .Include(x => x.User) // 🔥 xử lý luôn ở repo
                .Where(x => x.Id == id)
                .Select(x => new DiscussionResponseDto
                {
                    Id = x.Id,
                    ProblemId = x.ProblemId,
                    UserId = x.UserId,

                    // 🔥 chống null full
                    UserDisplayName =
                        x.User != null
                            ? (!string.IsNullOrEmpty(x.User.DisplayName)
                                ? x.User.DisplayName
                                : x.User.Username)
                            : "Unknown User",

                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,
                    Title = x.Title,
                    Content = x.Content,
                    IsPinned = x.IsPinned ?? false,
                    IsLocked = x.IsLocked ?? false,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();
        }
        public async Task LockAsync(Guid discussionId)
        {
            var discussion = await _db.ProblemDiscussions.FindAsync(discussionId);

            if (discussion == null)
                throw new Exception("Discussion not found");

            discussion.IsLocked = true;

            await _db.SaveChangesAsync();
        }

        public async Task UnlockAsync(Guid discussionId)
        {
            var discussion = await _db.ProblemDiscussions.FindAsync(discussionId);

            if (discussion == null)
                throw new Exception("Discussion not found");

            discussion.IsLocked = false;

            await _db.SaveChangesAsync();
        }

    }
}