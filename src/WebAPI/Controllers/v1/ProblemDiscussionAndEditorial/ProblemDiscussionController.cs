using Application.Common.Pagination;
using Application.UseCases.ProblemDiscussions.Commands;
using Application.UseCases.ProblemDiscussions.Dtos;
using Application.UseCases.ProblemDiscussions.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;
using MediatR;
using Application.Common.Interfaces;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial;

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

    // ===============================
    // GET: /api/v1/problems/{problemId}/discussions
    // ===============================
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

        var result = await _mediator.Send(
            new GetDiscussionsQuery(problemId, cursorCreatedAt, cursorId, pageSize), ct);

        return Ok(ApiResponse<CursorPaginationDto<DiscussionResponseDto>>
            .Ok(result, "Fetched discussions successfully"));
    }

    // ===============================
    // DELETE: /api/v1/discussions/{id}
    // ===============================
    [HttpDelete("/api/v{version:apiVersion}/discussions/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteDiscussion(
        Guid id,
        CancellationToken ct)
    {
        await _mediator.Send(new DeleteDiscussionCommand(id), ct);

        return Ok(ApiResponse<object>.Ok(true, "Discussion deleted successfully"));
    }

    // ===============================
    // GET: /api/v1/discussions/{id}
    // ===============================
    [HttpGet("/api/v{version:apiVersion}/discussions/{id:guid}")]
    public async Task<IActionResult> GetDiscussionById(
     Guid id,
     CancellationToken ct)
    {
        var discussion = await _discussionRepository.GetByIdWithUserAsync(id);

        if (discussion is null)
            return NotFound(new { Message = "Discussion not found." });

        return Ok(ApiResponse<DiscussionResponseDto>
            .Ok(discussion, "Fetched discussion successfully"));
    }

    // ===============================
    // POST: /api/v1/problems/{problemId}/discussions
    // ===============================
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

        // Fetch full response with user info
        var response = await _discussionRepository.GetByIdWithUserAsync(discussionId);

        return CreatedAtAction(
            nameof(GetDiscussionById),
            new { id = discussionId, version = "1.0" },
            ApiResponse<DiscussionResponseDto>.Ok(response!, "Discussion created successfully"));
    }
}