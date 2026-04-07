using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.DiscussionComments.Dtos;
using Application.UseCases.ProblemDiscussions.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        // ===============================
        // Get paged discussions (cursor pagination)
        // ===============================
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
                .ThenByDescending(x => x.d.CreatedAt.HasValue ? x.d.CreatedAt.Value : DateTime.MinValue)
                .ThenByDescending(x => x.d.Id);

            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                query = query.Where(x =>
                    (x.d.CreatedAt.HasValue ? x.d.CreatedAt.Value : DateTime.MinValue) < cursorCreatedAt.Value ||
                    ((x.d.CreatedAt.HasValue ? x.d.CreatedAt.Value : DateTime.MinValue) == cursorCreatedAt.Value &&
                     x.d.Id.CompareTo(cursorId.Value) < 0));
            }

            var list = await query
                .Take(pageSize + 1)
                .Select(x => new DiscussionResponseDto
                {
                    Id = x.d.Id,
                    ProblemId = x.d.ProblemId,
                    UserId = x.d.UserId,
                    UserDisplayName = x.u != null
                        ? (!string.IsNullOrEmpty(x.u.DisplayName) ? x.u.DisplayName : (x.u.Username ?? "Anonymous"))
                        : "Anonymous",
                    UserAvatarUrl = x.u != null ? x.u.AvatarUrl : null,
                    Title = x.d.Title,
                    Content = x.d.Content,
                    IsPinned = x.d.IsPinned ?? false,
                    IsLocked = x.d.IsLocked ?? false,
                    CreatedAt = x.d.CreatedAt.HasValue ? x.d.CreatedAt.Value : DateTime.MinValue
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

        // ===============================
        // Get discussion by ID (basic)
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
                        ? (!string.IsNullOrEmpty(x.User.DisplayName) ? x.User.DisplayName : (x.User.Username ?? "Anonymous"))
                        : "Anonymous",
                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,
                    Title = x.Title,
                    Content = x.Content,
                    IsPinned = x.IsPinned ?? false,
                    IsLocked = x.IsLocked ?? false,
                    CreatedAt = x.CreatedAt.HasValue ? x.CreatedAt.Value : DateTime.MinValue
                })
                .FirstOrDefaultAsync();
        }

        // ===============================
        // Get discussion with user info
        // ===============================
        public async Task<DiscussionResponseDto?> GetByIdWithUserAsync(Guid id)
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
                        ? (!string.IsNullOrEmpty(x.User.DisplayName) ? x.User.DisplayName : (x.User.Username ?? "Anonymous"))
                        : "Unknown User",
                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,
                    Title = x.Title,
                    Content = x.Content,
                    IsPinned = x.IsPinned ?? false,
                    IsLocked = x.IsLocked ?? false,
                    CreatedAt = x.CreatedAt.HasValue ? x.CreatedAt.Value : DateTime.MinValue
                })
                .FirstOrDefaultAsync();
        }

        // ===============================
        // Get discussion with comment tree
        // ===============================
        public async Task<DiscussionResponseDto?> GetDiscussionWithCommentsTreeAsync(Guid discussionId)
        {
            var discussion = await _db.ProblemDiscussions
                .Include(d => d.User)
                .Where(d => d.Id == discussionId)
                .Select(d => new DiscussionResponseDto
                {
                    Id = d.Id,
                    ProblemId = d.ProblemId,
                    UserId = d.UserId,
                    UserDisplayName = d.User != null
                        ? (!string.IsNullOrEmpty(d.User.DisplayName) ? d.User.DisplayName : (d.User.Username ?? "Anonymous"))
                        : "Anonymous",
                    UserAvatarUrl = d.User != null ? d.User.AvatarUrl : null,
                    Title = d.Title,
                    Content = d.Content,
                    IsPinned = d.IsPinned ?? false,
                    IsLocked = d.IsLocked ?? false,
                    CreatedAt = d.CreatedAt.HasValue ? d.CreatedAt.Value : DateTime.MinValue,
                    Comments = new List<DiscussionCommentResponseDto>()
                })
                .FirstOrDefaultAsync();

            if (discussion == null)
                return null;

            var comments = await _db.DiscussionComments
                .Include(c => c.User)
                .Where(c => c.DiscussionId == discussionId)
                .ToListAsync();

            var lookup = comments.ToLookup(c => c.ParentId);
            List<DiscussionCommentResponseDto> BuildTree(Guid? parentId)
            {
                return lookup[parentId]
                    .Select(c => new DiscussionCommentResponseDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserDisplayName = c.User != null
                            ? (!string.IsNullOrEmpty(c.User.DisplayName) ? c.User.DisplayName : (c.User.Username ?? "Anonymous"))
                            : "Anonymous",
                        UserAvatarUrl = c.User != null ? c.User.AvatarUrl : null,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt.HasValue ? c.CreatedAt.Value : DateTime.MinValue,
                        Children = BuildTree(c.Id)
                    }).ToList();
            }

            discussion.Comments = BuildTree(null);

            return discussion;
        }

        // ===============================
        // Lock / Unlock discussion
        // ===============================
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

        // ===============================
        // Dummy methods để implement interface
        // ===============================
        public Task<DiscussionResponseDto?> GetByIdWithVoteAndCommentsAsync(Guid discussionId, Guid? currentUserId)
        {
            throw new NotImplementedException();
        }
        public async Task DeleteDiscussionWithCommentsAsync(Guid discussionId)
        {
            // Lấy tất cả comment
            var comments = await _db.DiscussionComments
                .Where(c => c.DiscussionId == discussionId)
                .ToListAsync();

            // Xây cây comment: xóa từ con → cha
            var lookup = comments.ToLookup(c => c.ParentId);
            List<DiscussionComment> BuildTree(Guid? parentId)
            {
                return lookup[parentId]
                    .SelectMany(c => BuildTree(c.Id).Append(c))
                    .ToList();
            }

            var allComments = BuildTree(null);
            _db.DiscussionComments.RemoveRange(allComments);

            // Xóa discussion
            var discussion = await _db.ProblemDiscussions.FindAsync(discussionId);
            if (discussion != null)
                _db.ProblemDiscussions.Remove(discussion);

            await _db.SaveChangesAsync();
        }
    }
}