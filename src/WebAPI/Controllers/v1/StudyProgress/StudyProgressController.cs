using Application.UseCases.StudyProgress.Commands.CompleteProblem;
using Application.UseCases.StudyProgress.Commands.CompleteStudyPlanItem;
using Application.UseCases.StudyProgress.Commands.ResetStudyProgress;
using Application.UseCases.StudyProgress.Dtos;
using Application.UseCases.StudyProgress.Queries.GetMyStudyProgress;
using Application.UseCases.StudyProgress.Queries.GetNextStudyItem;
using Application.UseCases.StudyProgress.Queries.GetStudyPlanProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace WebAPI.Controllers.v1.StudyProgress;

[ApiController]
[Authorize]
[Route("api/study-progress")]
public class StudyProgressController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StudyProgressController> _logger;

    public StudyProgressController(
        IMediator mediator,
        ILogger<StudyProgressController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteProblemRequestDto request)
    {
        try
        {
            if (request == null || request.StudyPlanItemId == Guid.Empty)
            {
                return BadRequest(new
                {
                    message = "Invalid request"
                });
            }

            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized();

            var userId = Guid.Parse(userIdStr);

            await _mediator.Send(new CompleteProblemCommand
            {
                StudyPlanItemId = request.StudyPlanItemId,
                UserId = userId
            });

            return Ok(new
            {
                success = true,
                message = "Item completed",
                data = new
                {
                    request.StudyPlanItemId,
                    userId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompleteProblem error");

            return StatusCode(500, new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("plan/{planId}")]
    public async Task<IActionResult> GetPlanProgress(Guid planId)
    {
        try
        {
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized();

            var result = await _mediator.Send(new GetStudyPlanProgressQuery
            {
                StudyPlanId = planId,
                UserId = Guid.Parse(userIdStr)
            });

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("items/{studyPlanItemId}/complete")]
    public async Task<IActionResult> CompleteItem(Guid studyPlanItemId)
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized();

            var userId = Guid.Parse(userIdStr);

            await _mediator.Send(new CompleteStudyPlanItemCommand
            {
                UserId = userId,
                StudyPlanItemId = studyPlanItemId
            });

            return Ok(new
            {
                success = true,
                message = "Item completed",
                studyPlanItemId,
                userId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyProgress()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyStudyProgressQuery
        {
            UserId = Guid.Parse(userIdStr)
        });

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpDelete("{planId}")]
    public async Task<IActionResult> ResetProgress(Guid planId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        await _mediator.Send(new ResetStudyProgressCommand
        {
            UserId = Guid.Parse(userIdStr),
            StudyPlanId = planId
        });

        return Ok(new
        {
            success = true,
            message = "Progress reset successfully",
            planId
        });
    }

    [HttpGet("items/{itemId}/next")]
    public async Task<IActionResult> GetNextItem(Guid itemId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var result = await _mediator.Send(new GetNextStudyItemQuery
        {
            UserId = Guid.Parse(userIdStr),
            StudyPlanItemId = itemId
        });

        return Ok(new
        {
            success = true,
            data = result
        });
    }


}