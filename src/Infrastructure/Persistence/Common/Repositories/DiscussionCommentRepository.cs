using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class DiscussionCommentRepository : IDiscussionCommentRepository
    {
        private readonly TmojDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DiscussionCommentRepository(
            TmojDbContext db,
            ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // ===============================
        // GET BY ID (WITH USER + VOTE)
        // ===============================
        public async Task<CommentResponseDto?> GetByIdWithUserAsync(Guid id)
        {
            var userId = _currentUser.UserId;

            var comment = await _db.DiscussionComments
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.Id == id)
                .Select(x => new CommentResponseDto
                {
                    CommentId = x.Id,
                    DiscussionId = x.DiscussionId,
                    UserId = x.UserId,
                    ParentId = x.ParentId,

                    UserDisplayName =
                        x.User != null
                            ? (!string.IsNullOrEmpty(x.User.DisplayName)
                                ? x.User.DisplayName
                                : x.User.Username ?? "Anonymous")
                            : "Unknown User",

                    UserAvatarUrl = x.User != null ? x.User.AvatarUrl : null,

                    Content = x.Content,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue,

                    IsHidden = x.IsHidden ?? false, // 🔥 ADD

                    VoteCount = x.VoteCount,

                    Replies = new List<CommentResponseDto>()
                })
                .FirstOrDefaultAsync();

            if (comment == null) return null;

            var votes = await _db.CommentVotes
                .Where(v => v.CommentId == comment.CommentId)
                .ToListAsync();

            comment.TotalVotes = votes.Count;

            comment.UserVote = votes
                .Where(v => v.UserId == userId)
                .Select(v => (int?)v.Vote)
                .FirstOrDefault();

            // 🔥 APPLY HIDE LOGIC
            if (comment.IsHidden)
            {
                if (userId == null || comment.UserId != userId)
                {
                    comment.Content = "[Comment hidden]";
                }
            }

            return comment;
        }

        // ===============================
        // GET LIST BY DISCUSSION (WITH VOTE)
        // ===============================
        public async Task<List<CommentResponseDto>> GetByDiscussionIdAsync(Guid discussionId)
        {
            var userId = _currentUser.UserId;

            // ===============================
            // 1. GET COMMENTS (KHÔNG FILTER HIDDEN)
            // ===============================
            var flatList = await (
                from c in _db.DiscussionComments.AsNoTracking()
                join u in _db.Users on c.UserId equals u.UserId into gj
                from u in gj.DefaultIfEmpty()
                where c.DiscussionId == discussionId
                orderby c.CreatedAt
                select new CommentResponseDto
                {
                    CommentId = c.Id,
                    DiscussionId = c.DiscussionId,
                    UserId = c.UserId,
                    ParentId = c.ParentId,

                    Content = c.Content,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,

                    IsHidden = c.IsHidden ?? false, // 🔥 ADD

                    UserDisplayName =
                        u != null
                            ? (u.DisplayName ?? u.Username ?? "Anonymous")
                            : "Anonymous",

                    UserAvatarUrl = u != null ? u.AvatarUrl : null,

                    VoteCount = c.VoteCount,

                    Replies = new List<CommentResponseDto>()
                }
            ).ToListAsync();

            var commentIds = flatList.Select(x => x.CommentId).ToList();

            // ===============================
            // 2. GET ALL VOTES
            // ===============================
            var votes = await _db.CommentVotes
                .Where(v => commentIds.Contains(v.CommentId))
                .ToListAsync();

            var voteLookup = votes
                .GroupBy(v => v.CommentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var userVoteLookup = votes
                .Where(v => v.UserId == userId)
                .ToDictionary(v => v.CommentId, v => v.Vote);

            foreach (var c in flatList)
            {
                voteLookup.TryGetValue(c.CommentId, out var cVotes);

                c.TotalVotes = cVotes?.Count ?? 0;

                c.UserVote = userVoteLookup.TryGetValue(c.CommentId, out var uv)
                    ? uv
                    : null;
            }

            // ===============================
            // 3. APPLY HIDE LOGIC
            // ===============================
            foreach (var c in flatList)
            {
                if (c.IsHidden)
                {
                    if (userId == null || c.UserId != userId)
                    {
                        c.Content = "[Comment hidden]";
                    }
                }
            }

            // ===============================
            // 4. BUILD TREE
            // ===============================
            var lookup = flatList.ToDictionary(x => x.CommentId);
            var roots = new List<CommentResponseDto>();

            foreach (var comment in flatList)
            {
                if (comment.ParentId == null)
                {
                    roots.Add(comment);
                }
                else if (lookup.ContainsKey(comment.ParentId.Value))
                {
                    lookup[comment.ParentId.Value].Replies.Add(comment);
                }
            }

            return roots;
        }
        public async Task<DiscussionComment?> GetByIdWithDiscussionAsync(Guid id)
        {
            return await _db.DiscussionComments
                .Include(x => x.Discussion)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}