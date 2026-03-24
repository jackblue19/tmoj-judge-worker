using Domain.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    [Route("api/v1/problem-discussion")]
    [ApiController]
    public class ProblemDiscussion : ControllerBase
    {
        private readonly TmojDbContext _db;

        public ProblemDiscussion(TmojDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // CURSOR PAGINATION
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize = 10)
        {
            var query = _db.ProblemDiscussions
                .AsNoTracking()
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .AsQueryable();

            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt < cursorCreatedAt ||
                    (x.CreatedAt == cursorCreatedAt &&
                     x.Id.CompareTo(cursorId.Value) < 0));
            }

            var discussions = await query
                .Take(pageSize + 1)
                .Select(d => new DiscussionResponseDto
                {
                    Id = d.Id,
                    ProblemId = d.ProblemId,
                    UserId = d.UserId,
                    Title = d.Title,
                    Content = d.Content,
                    IsPinned = d.IsPinned ?? false,
                    IsLocked = d.IsLocked ?? false,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            var hasMore = discussions.Count > pageSize;

            if (hasMore)
                discussions.RemoveAt(pageSize);

            var last = discussions.LastOrDefault();

            return Ok(new CursorPaginationDto<DiscussionResponseDto>
            {
                Items = discussions,
                NextCursorCreatedAt = last?.CreatedAt,
                NextCursorId = last?.Id,
                HasMore = hasMore
            });
        }

        // =====================================================
        // CREATE DISCUSSION
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> Create(CreateDiscussionDto dto)
        {
            if (!await _db.Problems.AnyAsync(p => p.Id == dto.ProblemId))
                return BadRequest("Problem not found");

            if (!await _db.Users.AnyAsync(u => u.UserId == dto.UserId))
                return BadRequest("User not found");

            var entity = new Domain.Entities.ProblemDiscussion
            {
                Id = Guid.NewGuid(),
                ProblemId = dto.ProblemId,
                UserId = dto.UserId,
                Title = dto.Title,
                Content = dto.Content,
                CreatedAt = DateTimeHelper.Now(),
                UpdatedAt = DateTimeHelper.Now(),
                IsPinned = false,
                IsLocked = false
            };

            _db.ProblemDiscussions.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(entity);
        }

        // =====================================================
        // CREATE COMMENT
        // =====================================================
        [HttpPost("comment")]
        public async Task<IActionResult> CreateComment(CreateCommentDto dto)
        {
            if (!await _db.ProblemDiscussions
                .AnyAsync(x => x.Id == dto.DiscussionId))
                return BadRequest("Discussion not found");

            // kiểm tra depth
            if (dto.ParentId != null)
            {
                var parent = await _db.DiscussionComments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.ParentId);

                if (parent == null)
                    return BadRequest("Parent comment not found");

                // check grandparent
                if (parent.ParentId != null)
                {
                    var grandParent = await _db.DiscussionComments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == parent.ParentId);

                    if (grandParent?.ParentId != null)
                        return BadRequest("Max comment depth is 3");
                }
            }

            var comment = new DiscussionComment
            {
                Id = Guid.NewGuid(),
                DiscussionId = dto.DiscussionId,
                UserId = dto.UserId,
                Content = dto.Content,
                ParentId = dto.ParentId,
                CreatedAt = DateTimeHelper.Now(),
                UpdatedAt = DateTimeHelper.Now()
            };

            _db.DiscussionComments.Add(comment);

            await _db.SaveChangesAsync();

            return Ok(comment);
        }

        // =====================================================
        // DELETE COMMENT
        // =====================================================
        [Authorize]
        [HttpDelete("comment/{id}")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var comment = await _db.DiscussionComments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (comment == null)
                return NotFound("Comment not found");

            // chỉ owner hoặc admin được xoá
            if (comment.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // nếu là root comment -> xoá cả discussion
            if (comment.ParentId == null)
            {
                return await DeleteDiscussion(comment.DiscussionId);
            }

            await DeleteCommentRecursive(id);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Comment deleted"
            });
        }

        private async Task DeleteCommentRecursive(Guid commentId)
        {
            var children = await _db.DiscussionComments
                .Where(x => x.ParentId == commentId)
                .ToListAsync();

            foreach (var child in children)
            {
                await DeleteCommentRecursive(child.Id);
            }

            var votes = await _db.CommentVotes
                .Where(x => x.CommentId == commentId)
                .ToListAsync();

            _db.CommentVotes.RemoveRange(votes);

            var comment = await _db.DiscussionComments.FindAsync(commentId);

            if (comment != null)
                _db.DiscussionComments.Remove(comment);
        }

        // =====================================================
        // VOTE COMMENT
        // =====================================================
        [Authorize]
        [HttpPost("comments/vote")]
        public async Task<IActionResult> VoteComment([FromBody] VoteCommentDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var comment = await _db.DiscussionComments
                .FirstOrDefaultAsync(x => x.Id == dto.CommentId);

            if (comment == null)
                return NotFound("Comment not found");

            var existingVote = await _db.CommentVotes
                .FirstOrDefaultAsync(x =>
                    x.CommentId == dto.CommentId &&
                    x.UserId == userId);

            if (existingVote == null)
            {
                if (dto.Vote == 0)
                    return Ok();

                _db.CommentVotes.Add(new CommentVote
                {
                    Id = Guid.NewGuid(),
                    CommentId = dto.CommentId,
                    UserId = userId,
                    Vote = dto.Vote,
                    CreatedAt = DateTime.UtcNow
                });

                comment.VoteCount += dto.Vote;
            }
            else
            {
                comment.VoteCount -= existingVote.Vote;

                if (dto.Vote == 0)
                {
                    _db.CommentVotes.Remove(existingVote);
                }
                else
                {
                    existingVote.Vote = dto.Vote;
                    comment.VoteCount += dto.Vote;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                voteCount = comment.VoteCount
            });
        }

        // =====================================================
        // HIDE COMMENT (ADMIN)
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpPost("comments/hide")]
        public async Task<IActionResult> HideComment([FromBody] HideCommentDto dto)
        {
            var comment = await _db.DiscussionComments
                .FirstOrDefaultAsync(x => x.Id == dto.CommentId);

            if (comment == null)
                return NotFound();

            comment.Content = "[hidden by admin]";
            comment.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok();
        }

        // =====================================================
        // EDIT COMMENT (PRIVATE)
        // =====================================================

        [Authorize]
        [HttpPut("comment")]
        public async Task<IActionResult> UpdateComment([FromBody] UpdateCommentDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var comment = await _db.DiscussionComments
                .FirstOrDefaultAsync(x => x.Id == dto.CommentId);

            if (comment == null)
                return NotFound("Comment not found");

            // chỉ owner được edit
            if (comment.UserId != userId)
                return Forbid();

            comment.Content = dto.Content;
            comment.UpdatedAt = DateTimeHelper.Now();

            await _db.SaveChangesAsync();

            return Ok(new
            {
                comment.Id,
                comment.Content,
                comment.UpdatedAt
            });
       
        }

        // =====================================================
        // DELETE DISCUSSION (CASCADE DELETE COMMENTS)
        // =====================================================

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscussion(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var discussion = await _db.ProblemDiscussions
                .FirstOrDefaultAsync(x => x.Id == id);

            if (discussion == null)
                return NotFound("Discussion not found");

            // chỉ owner hoặc admin được xoá
            if (discussion.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var comments = await _db.DiscussionComments
                .Where(x => x.DiscussionId == id)
                .ToListAsync();

            var commentIds = comments.Select(x => x.Id).ToList();

            var votes = await _db.CommentVotes
                .Where(x => commentIds.Contains(x.CommentId))
                .ToListAsync();

            _db.CommentVotes.RemoveRange(votes);
            _db.DiscussionComments.RemoveRange(comments);
            _db.ProblemDiscussions.Remove(discussion);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Discussion deleted"
            });
        }

        // =====================================================
        // GET DISCUSSION DETAIL + COMMENT TREE
        // =====================================================
        [HttpGet("{discussionId}")]
        public async Task<IActionResult> GetDiscussion(Guid discussionId)
        {
            var discussion = await _db.ProblemDiscussions
                .AsNoTracking()
                .Where(d => d.Id == discussionId)
                .Select(d => new DiscussionResponseDto
                {
                    Id = d.Id,
                    ProblemId = d.ProblemId,
                    UserId = d.UserId,
                    Title = d.Title,
                    Content = d.Content,
                    IsPinned = d.IsPinned ?? false,
                    IsLocked = d.IsLocked ?? false,
                    CreatedAt = d.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (discussion == null)
                return NotFound("Discussion not found");

            var comments = await _db.DiscussionComments
                .AsNoTracking()
                .Where(x => x.DiscussionId == discussionId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            var commentTree = BuildCommentTree(comments);

            return Ok(new
            {
                discussion,
                comments = commentTree
            });
        }
        private List<CommentResponseDto> BuildCommentTree(List<DiscussionComment> comments)
        {
            var lookup = comments.ToDictionary(
                c => c.Id,
                c => new CommentResponseDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Content = c.Content,
                    VoteCount = c.VoteCount,
                    ParentId = c.ParentId,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                    Replies = new List<CommentResponseDto>()
                });

            var roots = new List<CommentResponseDto>();

            foreach (var comment in lookup.Values)
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
    }


}

     
