using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.DiscussionComments.Dtos;
using Application.UseCases.ProblemDiscussions.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class ProblemDiscussionRepository : IProblemDiscussionRepository
    {
        private readonly TmojDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ProblemDiscussionRepository(TmojDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // ===============================
        // PAGED LIST
        // ===============================
        public async Task<CursorPaginationDto<DiscussionResponseDto>> GetPagedAsync(
            Guid problemId,
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

            var query = _db.ProblemDiscussions
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => d.ProblemId == problemId);

            // Visibility Filter
            if (!isAdmin)
            {
                query = query.Where(d => d.IsHidden != true || d.UserId == userId);
            }

            query = query
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id);

            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                query = query.Where(x =>
                    (x.CreatedAt ?? DateTime.MinValue) < cursorCreatedAt.Value ||
                    ((x.CreatedAt ?? DateTime.MinValue) == cursorCreatedAt.Value &&
                     x.Id.CompareTo(cursorId.Value) < 0));
            }

            var list = await query
                .Take(pageSize + 1)
                .Select(x => new DiscussionResponseDto
                {
                    Id = x.Id,
                    ProblemId = x.ProblemId,
                    UserId = x.UserId,
                    UserDisplayName = x.User != null
                        ? (x.User.DisplayName ?? x.User.Username ?? "Anonymous")
                        : "Anonymous",
                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,
                    Title = x.Title,
                    Content = x.Content,
                    IsPinned = x.IsPinned ?? false,
                    IsLocked = x.IsLocked ?? false,
                    IsHidden = x.IsHidden ?? false,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            var hasMore = list.Count > pageSize;
            if (hasMore) list.RemoveAt(pageSize);

            var last = list.LastOrDefault();

            return new CursorPaginationDto<DiscussionResponseDto>
            {
                Items = list,
                NextCursorCreatedAt = last?.CreatedAt,
                NextCursorId = last?.Id,
                HasMore = hasMore
            };
        }

        public async Task<CursorPaginationDto<DiscussionResponseDto>> GetPagedByUserAsync(
            Guid userId,
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize)
        {
            var query = _db.ProblemDiscussions
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => d.UserId == userId);

            query = query
                .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id);

            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                query = query.Where(x =>
                    (x.CreatedAt ?? DateTime.MinValue) < cursorCreatedAt.Value ||
                    ((x.CreatedAt ?? DateTime.MinValue) == cursorCreatedAt.Value &&
                     x.Id.CompareTo(cursorId.Value) < 0));
            }

            var list = await query
                .Take(pageSize + 1)
                .Select(x => new DiscussionResponseDto
                {
                    Id = x.Id,
                    ProblemId = x.ProblemId,
                    UserId = x.UserId,
                    UserDisplayName = x.User != null
                        ? (x.User.DisplayName ?? x.User.Username ?? "Anonymous")
                        : "Anonymous",
                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,
                    Title = x.Title,
                    Content = x.Content,
                    IsPinned = x.IsPinned ?? false,
                    IsLocked = x.IsLocked ?? false,
                    IsHidden = x.IsHidden ?? false,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            var hasMore = list.Count > pageSize;
            if (hasMore) list.RemoveAt(pageSize);

            var last = list.LastOrDefault();

            return new CursorPaginationDto<DiscussionResponseDto>
            {
                Items = list,
                NextCursorCreatedAt = last?.CreatedAt,
                NextCursorId = last?.Id,
                HasMore = hasMore
            };
        }

        // ===============================
        // GET DTO (🔥 FIX)
        // ===============================
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
                    UserDisplayName = x.User != null
                        ? (x.User.DisplayName ?? x.User.Username ?? "Anonymous")
                        : "Anonymous",
                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,
                    Title = x.Title,
                    Content = x.Content,
                    IsPinned = x.IsPinned ?? false,
                    IsLocked = x.IsLocked ?? false,
                    IsHidden = x.IsHidden ?? false,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue
                })
                .FirstOrDefaultAsync();
        }

        // ===============================
        // GET ENTITY (🔥 FIX CHUẨN CLEAN ARCH)
        // ===============================
        public async Task<ProblemDiscussion?> GetEntityByIdAsync(Guid id)
        {
            return await _db.ProblemDiscussions
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<DiscussionResponseDto?> GetByIdWithUserAsync(Guid id)
        {
            return await GetByIdAsync(id);
        }

        // ===============================
        // DISCUSSION + COMMENTS TREE
        // ===============================
        public async Task<DiscussionResponseDto?> GetDiscussionWithCommentsTreeAsync(Guid discussionId)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

            var discussion = await GetByIdAsync(discussionId);
            if (discussion == null) return null;

            // Visibility Check for Discussion
            bool canSeeHidden = isAdmin || discussion.UserId == userId;
            if (discussion.IsHidden && !canSeeHidden)
            {
                discussion.Title = "[Discussion hidden]";
                discussion.Content = "[This discussion has been hidden by moderation]";
            }

            var comments = await _db.DiscussionComments
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.DiscussionId == discussionId)
                .ToListAsync();

            var lookup = comments.ToLookup(c => c.ParentId);

            List<DiscussionCommentResponseDto> Build(Guid? parentId)
            {
                return lookup[parentId]
                    .Select(c =>
                    {
                        var dto = new DiscussionCommentResponseDto
                        {
                            Id = c.Id,
                            UserId = c.UserId,
                            UserDisplayName = c.User != null
                                ? (c.User.DisplayName ?? c.User.Username ?? "Anonymous")
                                : "Anonymous",
                            UserAvatarUrl = c.User != null ? c.User.AvatarUrl : null,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                            IsHidden = c.IsHidden ?? false
                        };

                        // Hide content if discussion is hidden OR comment itself is hidden
                        if ((discussion.IsHidden || dto.IsHidden) && !canSeeHidden && dto.UserId != userId)
                        {
                            dto.Content = "[Comment hidden]";
                        }

                        dto.Children = Build(c.Id);
                        return dto;
                    }).ToList();
            }

            discussion.Comments = Build(null);
            return discussion;
        }

        public async Task<DiscussionResponseDto?> GetByIdWithVoteAndCommentsAsync(Guid discussionId, Guid? userId)
        {
            return await GetDiscussionWithCommentsTreeAsync(discussionId);
        }

        // ===============================
        // DELETE
        // ===============================
        public async Task DeleteDiscussionWithCommentsAsync(Guid discussionId)
        {
            var comments = await _db.DiscussionComments
                .Where(c => c.DiscussionId == discussionId)
                .ToListAsync();

            _db.DiscussionComments.RemoveRange(comments);

            var discussion = await _db.ProblemDiscussions.FindAsync(discussionId);
            if (discussion != null)
                _db.ProblemDiscussions.Remove(discussion);
        }

        // ===============================
        // LOCK / UNLOCK (NO SAVE)
        // ===============================
        public async Task LockAsync(Guid discussionId)
        {
            var discussion = await _db.ProblemDiscussions.FindAsync(discussionId);
            if (discussion == null)
                throw new Exception("Discussion not found");

            discussion.IsLocked = true;
        }

        public async Task UnlockAsync(Guid discussionId)
        {
            var discussion = await _db.ProblemDiscussions.FindAsync(discussionId);
            if (discussion == null)
                throw new Exception("Discussion not found");

            discussion.IsLocked = false;
        }
        public async Task<List<ProblemDiscussion>> GetDiscussionEntitiesByIdsAsync(List<Guid> ids)
        {
            return await _db.ProblemDiscussions
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        public async Task<List<UserActivityDto>> GetUserActivitiesAsync(Guid userId, int limit)
        {
            var discussions = await _db.ProblemDiscussions
                .AsNoTracking()
                .Include(d => d.Problem)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Take(limit)
                .Select(d => new UserActivityDto
                {
                    Id = d.Id,
                    DiscussionId = d.Id,
                    ProblemId = d.ProblemId,
                    ProblemTitle = d.Problem != null ? d.Problem.Title : "Unknown Problem",
                    Type = "discussion",
                    Title = d.Title,
                    Content = d.Content,
                    CreatedAt = d.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            var comments = await _db.DiscussionComments
                .AsNoTracking()
                .Include(c => c.Discussion)
                    .ThenInclude(d => d.Problem)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .Select(c => new UserActivityDto
                {
                    Id = c.Id,
                    DiscussionId = c.DiscussionId,
                    ProblemId = c.Discussion != null ? c.Discussion.ProblemId : Guid.Empty,
                    ProblemTitle = (c.Discussion != null && c.Discussion.Problem != null) ? c.Discussion.Problem.Title : "Unknown Problem",
                    Type = "comment",
                    Title = "Commented on: " + (c.Discussion != null ? c.Discussion.Title : "Deleted Discussion"),
                    Content = c.Content,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            var combined = discussions.Concat(comments)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToList();

            return combined;
        }
    }
}