using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Dtos;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class DiscussionCommentRepository : IDiscussionCommentRepository
    {
        private readonly TmojDbContext _db;

        public DiscussionCommentRepository(TmojDbContext db)
        {
            _db = db;
        }

        // ===============================
        // GET BY ID (WITH USER)
        // ===============================
        public async Task<CommentResponseDto?> GetByIdWithUserAsync(Guid id)
        {
            return await _db.DiscussionComments
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.Id == id)
                .Select(x => new CommentResponseDto
                {
                    CommentId = x.Id,
                    DiscussionId = x.DiscussionId,
                    UserId = x.UserId,

                    ParentId = x.ParentId, // 🔥 FIX

                    UserDisplayName =
                        x.User != null
                            ? (!string.IsNullOrEmpty(x.User.DisplayName)
                                ? x.User.DisplayName
                                : x.User.Username ?? "Anonymous")
                            : "Unknown User",

                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,

                    Content = x.Content,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue,

                    Replies = new List<CommentResponseDto>()
                })
                .FirstOrDefaultAsync();
        }

        // ===============================
        // GET LIST BY DISCUSSION
        // ===============================
        public async Task<List<CommentResponseDto>> GetByDiscussionIdAsync(Guid discussionId)
        {
            // 🔥 1 QUERY DUY NHẤT
            var flatList = await (
                from c in _db.DiscussionComments.AsNoTracking()
                join u in _db.Users on c.UserId equals u.UserId into gj
                from u in gj.DefaultIfEmpty()
                where c.DiscussionId == discussionId
                      && (c.IsHidden == null || c.IsHidden == false)
                orderby c.CreatedAt
                select new CommentResponseDto
                {
                    CommentId = c.Id,
                    DiscussionId = c.DiscussionId,
                    UserId = c.UserId,

                    ParentId = c.ParentId, // 🔥 QUAN TRỌNG

                    Content = c.Content,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,

                    UserDisplayName =
                        u != null
                            ? (u.DisplayName ?? u.Username ?? "Anonymous")
                            : "Anonymous",

                    UserAvatarUrl = u != null ? u.AvatarUrl : null,

                    Replies = new List<CommentResponseDto>()
                }
            ).ToListAsync();

            // ===============================
            // BUILD TREE
            // ===============================
            var lookup = flatList.ToDictionary(x => x.CommentId);
            var roots = new List<CommentResponseDto>();

            foreach (var comment in flatList)
            {
                if (comment.ParentId == null)
                {
                    // root
                    roots.Add(comment);
                }
                else if (lookup.ContainsKey(comment.ParentId.Value))
                {
                    // nested reply (multi-level support)
                    lookup[comment.ParentId.Value].Replies.Add(comment);
                }
            }

            return roots;
        }
    }
}