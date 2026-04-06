using Application.UseCases.DiscussionComments.Commands;
using Application.UseCases.DiscussionComments.Dtos;
using Application.UseCases.DiscussionComments.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class DiscussionCommentController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscussionCommentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/v1/discussions/{id}/comments
    [HttpPost("discussions/{id:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> CreateComment(
        Guid id,
        [FromBody] CommentCreateDto dto,
        CancellationToken ct)
    {
        var commentId = await _mediator.Send(
            new CreateDiscussionCommentCommand(id, dto.Content, dto.ParentId), ct);

        return Ok(ApiResponse<object>.Ok(
            new { CommentId = commentId },
            "Comment created successfully"));
    }

    // PUT /api/v1/comments/{id}
    [HttpPut("comments/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateComment(
        Guid id,
        [FromBody] CommentUpdateDto dto,
        CancellationToken ct)
    {
        await _mediator.Send(
            new UpdateDiscussionCommentCommand(id, dto.Content), ct);

        return Ok(ApiResponse<bool>.Ok(true, "Comment updated successfully"));
    }

    // DELETE /api/v1/comments/{id}
    [HttpDelete("comments/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(
        Guid id,
        CancellationToken ct)
    {
        await _mediator.Send(
            new DeleteCommentCommand(id), ct);

        return Ok(ApiResponse<object>.Ok(new {}, "Comment deleted successfully"));
    }

    // ===============================
    // GET: /api/v1/discussions/{id}/comments (nested 1-level)
    // ===============================
    [HttpGet("/api/v{version:apiVersion}/discussions/{id:guid}/comments")]
    public async Task<IActionResult> GetComments(
        Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetCommentsByDiscussionQuery(id), ct);

        return Ok(ApiResponse<List<CommentResponseDto>>
            .Ok(result, "Fetched comments successfully"));
    }
    // ===============================
    // POST: /api/v1/comments/{id}/vote
    // ===============================
    [HttpPost("comments/{id:guid}/vote")]
    [Authorize]
    public async Task<IActionResult> VoteComment(
      Guid id,
      [FromBody] VoteCommentDto dto,
      CancellationToken ct)
    {
        // Map message dựa trên voteType
        string action = dto.VoteType switch
        {
            1 => "You upvote",
            -1 => "You downvote",
            0 => "You unvote",
            _ => "Vote success"
        };

        // Gọi handler
        await _mediator.Send(new VoteCommentCommand(id, dto.VoteType), ct);

        return Ok(ApiResponse<object>.Ok(new {}, action));
    }
    // ===============================
    // POST: /api/v1/comments/{id}/Hide/Unhide
    // ===============================
    [HttpPost("comments/{id:guid}/hide")]
    [Authorize]
    public async Task<IActionResult> HideUnhideComment(
     Guid id,
     [FromBody] HideUnhideCommentDto dto,
     CancellationToken ct)
    {
        var result = await _mediator.Send(new HideUnhideCommentCommand(id, dto.Hide), ct);

        var action = dto.Hide ? "Comment hidden" : "Comment unhidden";

        return Ok(ApiResponse<object>.Ok(new {}, action));
    }


}
