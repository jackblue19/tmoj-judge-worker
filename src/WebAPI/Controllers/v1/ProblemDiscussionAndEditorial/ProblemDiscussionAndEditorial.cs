using Domain.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Controllers.v1.ProblemDiscussionAndEditorial;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    [Route("api/v1/problem-discussion")]
    [ApiController]
    public class ProblemDiscussionAndEditorial : ControllerBase
    {
        private readonly TmojDbContext _db;

        public ProblemDiscussionAndEditorial(TmojDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // ✅ STABLE CURSOR PAGINATION (LEETCODE STYLE)
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

            // ✅ composite cursor
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

            var entity = new ProblemDiscussion
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
        // CREATE COMMENT (LEVEL = 1)
        // =====================================================
        [HttpPost("comment")]
        public async Task<IActionResult> CreateComment(CreateCommentDto dto)
        {
            if (!await _db.ProblemDiscussions
                .AnyAsync(x => x.Id == dto.DiscussionId))
                return BadRequest("Discussion not found");

            // limit reply depth
            if (dto.ParentId != null)
            {
                var parent = await _db.DiscussionComments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.ParentId);

                if (parent == null)
                    return BadRequest("Parent not found");

                if (parent.ParentId != null)
                    return BadRequest("Only 1 reply level allowed");
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
        // DISCUSSION DETAIL + COMMENT TREE
        // =====================================================
        [HttpGet("{discussionId}")]
        public async Task<IActionResult> GetDiscussionDetail(Guid discussionId)
        {
            var discussion = await _db.ProblemDiscussions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == discussionId);

            if (discussion == null)
                return NotFound();

            var comments = await _db.DiscussionComments
                .AsNoTracking()
                .Where(x => x.DiscussionId == discussionId)
                .OrderBy(x => x.CreatedAt)
                .Select(c => new CommentTreeDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Content = c.Content,
                    ParentId = c.ParentId,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var lookup = comments.ToDictionary(x => x.Id);
            var roots = new List<CommentTreeDto>();

            foreach (var comment in comments)
            {
                if (comment.ParentId == null)
                    roots.Add(comment);
                else if (lookup.TryGetValue(
                    comment.ParentId.Value,
                    out var parent))
                    parent.Replies.Add(comment);
            }

            return Ok(new DiscussionDetailDto
            {
                Id = discussion.Id,
                ProblemId = discussion.ProblemId,
                UserId = discussion.UserId,
                Title = discussion.Title,
                Content = discussion.Content,
                CreatedAt = discussion.CreatedAt,
                Comments = roots
            });
        }

        // =====================================================
        // ✅ DELETE COMMENT (DB CASCADE SAFE)
        // =====================================================
        [HttpDelete("comment/{id}")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var comment =
                await _db.DiscussionComments.FindAsync(id);

            if (comment == null)
                return NotFound();

            _db.DiscussionComments.Remove(comment);

            await _db.SaveChangesAsync();

            return Ok("Deleted");
        }

        // =====================================================
        // EDITORIAL CRUD
        // =====================================================
        [HttpGet("editorial")]
        public async Task<IActionResult> GetEditorials()
        {
            return Ok(await _db.ProblemEditorials
                .AsNoTracking()
                .ToListAsync());
        }

        [HttpPost("editorial")]
        public async Task<IActionResult> CreateEditorial(CreateEditorialDto dto)
        {
            var entity = new ProblemEditorial
            {
                Id = Guid.NewGuid(),
                ProblemId = dto.ProblemId,
                AuthorId = dto.AuthorId,
                Content = dto.Content,
                CreatedAt = DateTimeHelper.Now(),
                UpdatedAt = DateTimeHelper.Now()
            };

            _db.ProblemEditorials.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpPut("editorial/{id}")]
        public async Task<IActionResult> UpdateEditorial(
            Guid id,
            UpdateEditorialDto dto)
        {
            var entity =
                await _db.ProblemEditorials.FindAsync(id);

            if (entity == null)
                return NotFound();

            entity.Content = dto.Content;
            entity.UpdatedAt = DateTimeHelper.Now();

            await _db.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpDelete("editorial/{id}")]
        public async Task<IActionResult> DeleteEditorial(Guid id)
        {
            var entity =
                await _db.ProblemEditorials.FindAsync(id);

            if (entity == null)
                return NotFound();

            _db.ProblemEditorials.Remove(entity);
            await _db.SaveChangesAsync();

            return Ok("Deleted");
        }
    }
}