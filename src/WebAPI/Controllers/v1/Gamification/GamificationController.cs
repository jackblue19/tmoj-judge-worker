using Application.Common.Events;
using Application.UseCases.Gamification.Events;
using Application.UseCases.Gamification.Queries.GetLeaderboard;
using Application.UseCases.Gamification.Queries.GetMyBadgeProgress;
using Application.UseCases.Gamification.Queries.GetMyBadges;
using Application.UseCases.Gamification.Queries.GetMyGamification;
using Application.UseCases.Gamification.Queries.GetMyRewardHistory;
using Application.UseCases.Gamification.Queries.GetMyStreak;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.v1.Gamification;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gamification")]
public class GamificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public GamificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =====================================================
    // GET MY GAMIFICATION
    // =====================================================
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyGamification(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyGamificationQuery(), ct);

        return Ok(WebAPI.Models.Common.ApiResponse<object>.Ok(
            result,
            "Fetched gamification data successfully"
        ));
    }

    // =====================================================
    // GET MY BADGES
    // =====================================================
    [HttpGet("badges")]
    [Authorize]
    public async Task<IActionResult> GetMyBadges(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyBadgesQuery(), ct);

        return Ok(WebAPI.Models.Common.ApiResponse<object>.Ok(
            result,
            "Fetched user badges successfully"
        ));
    }

    // =====================================================
    // GET BADGE PROGRESS
    // =====================================================
    [HttpGet("badges/progress")]
    [Authorize]
    public async Task<IActionResult> GetBadgeProgress(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyBadgeProgressQuery(), ct);

        return Ok(WebAPI.Models.Common.ApiResponse<object>.Ok(
            result,
            "Fetched badge progress successfully"
        ));
    }

    // =====================================================
    // GET MY STREAK
    // =====================================================
    [HttpGet("streak")]
    [Authorize]
    public async Task<IActionResult> GetMyStreak(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyStreakQuery(), ct);

        return Ok(WebAPI.Models.Common.ApiResponse<object>.Ok(
            result,
            "Fetched user streak successfully"
        ));
    }

    // =====================================================
    // GET REWARD HISTORY
    // =====================================================
    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetRewardHistory(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyRewardHistoryQuery(), ct);

        return Ok(WebAPI.Models.Common.ApiResponse<object>.Ok(
            result,
            "Fetched reward history successfully"
        ));
    }

    // =====================================================
    // LEADERBOARD
    // =====================================================
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string type = "exp",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetLeaderboardQuery { Type = type }, ct);

        return Ok(WebAPI.Models.Common.ApiResponse<object>.Ok(
            result,
            "Fetched leaderboard successfully"
        ));
    }

    // =====================================================
    // DEBUG: TRIGGER AC EVENT
    // =====================================================
    [HttpPost("test/ac")]
    public async Task<IActionResult> TestAc(Guid userId, Guid problemId)
    {
        await _mediator.Publish(
            new SubmissionAcceptedEvent(userId, problemId)
        );

        return Ok(new
        {
            message = "AC event triggered",
            userId,
            problemId
        });
    }
}