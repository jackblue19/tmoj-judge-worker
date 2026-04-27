using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.DiscussionComments.Dtos;
using Application.UseCases.ProblemDiscussions.Commands;
using Application.UseCases.ProblemDiscussions.Dtos;
using Application.UseCases.ProblemDiscussions.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/problems/{problemId:guid}/discussions")]
    public class ProblemDiscussionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IProblemDiscussionRepository _discussionRepository;

        public ProblemDiscussionController(
            IMediator mediator,
            IProblemDiscussionRepository discussionRepository)
        {
            _mediator = mediator;
            _discussionRepository = discussionRepository;
        }

        // GET list discussions (cursor pagination)
        [HttpGet]
        public async Task<ActionResult<CursorPaginationDto<DiscussionResponseDto>>> GetDiscussions(
            Guid problemId,
            [FromQuery] DateTime? cursorCreatedAt,
            [FromQuery] Guid? cursorId,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            if (pageSize <= 0 || pageSize > 50)
                return BadRequest(new { Message = "pageSize must be between 1 and 50" });

            var result = await _discussionRepository.GetPagedAsync(
                problemId, cursorCreatedAt, cursorId, pageSize);

            return Ok(ApiResponse<CursorPaginationDto<DiscussionResponseDto>>
                .Ok(result, "Fetched discussions successfully"));
        }

        // GET discussion by ID + comment tree
        [HttpGet("/api/v{version:apiVersion}/discussions/{id:guid}")]
        public async Task<IActionResult> GetDiscussionById(
            Guid id,
            CancellationToken ct)
        {
            var discussion = await _discussionRepository.GetDiscussionWithCommentsTreeAsync(id);

            if (discussion is null)
                return NotFound(new { Message = "Discussion not found." });

            return Ok(ApiResponse<DiscussionResponseDto>
                .Ok(discussion, "Fetched discussion successfully"));
        }
 
        // GET my discussions
        [HttpGet("/api/v{version:apiVersion}/discussions/me")]
        [Authorize]
        public async Task<ActionResult<CursorPaginationDto<DiscussionResponseDto>>> GetMyDiscussions(
            [FromServices] ICurrentUserService currentUserService,
            [FromQuery] DateTime? cursorCreatedAt,
            [FromQuery] Guid? cursorId,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var userId = currentUserService.UserId;
            if (userId == null || userId == Guid.Empty) return Unauthorized();

            var result = await _mediator.Send(
                new GetMyDiscussionsQuery(userId.Value, cursorCreatedAt, cursorId, pageSize), ct);

            return Ok(ApiResponse<CursorPaginationDto<DiscussionResponseDto>>
                .Ok(result, "Fetched your discussions successfully"));
        }

        // POST create discussion
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateDiscussion(
            Guid problemId,
            [FromBody] DiscussionCreateDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { Message = "Title is required." });

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { Message = "Content is required." });

            var discussionId = await _mediator.Send(
                new CreateDiscussionCommand(problemId, dto.Title, dto.Content), ct);

            var response = await _discussionRepository.GetDiscussionWithCommentsTreeAsync(discussionId);

            return CreatedAtAction(
                nameof(GetDiscussionById),
                new { id = discussionId, version = "1.0" },
                ApiResponse<DiscussionResponseDto>.Ok(response!, "Discussion created successfully"));
        }

        // DELETE discussion
        [HttpDelete("/api/v{version:apiVersion}/discussions/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteDiscussion(
            Guid id,
            CancellationToken ct)
        {
            await _mediator.Send(new DeleteDiscussionCommand(id), ct);

            return Ok(ApiResponse<object>.Ok(true, "Discussion deleted successfully"));
        }

        // POST vote discussion
        [HttpPost("/api/v{version:apiVersion}/discussions/{id:guid}/vote")]
        [Authorize]
        public async Task<IActionResult> VoteDiscussion(
            Guid id,
            [FromBody] VoteCommentDto dto,
            CancellationToken ct)
        {
            string action = dto.VoteType switch
            {
                1 => "You upvoted discussion",
                -1 => "You downvoted discussion",
                0 => "You removed your vote",
                _ => "Vote success"
            };

            await _mediator.Send(
                new VoteDiscussionCommand(id, dto.VoteType), ct);

            return Ok(ApiResponse<object?>.Ok(null, action));
        }

        // PUT update discussion
        [HttpPut("/api/v{version:apiVersion}/discussions/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateDiscussion(
            Guid id,
            [FromBody] DiscussionUpdateDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { Message = "Title is required." });

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { Message = "Content is required." });

            await _mediator.Send(
                new UpdateDiscussionCommand(id, dto.Title, dto.Content), ct);

            return Ok(ApiResponse<object?>.Ok(null, "Discussion updated successfully"));
        }
    }
}