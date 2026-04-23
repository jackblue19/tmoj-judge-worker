using Application.Common.Events;
using Application.UseCases.Gamification.Commands.CreateBadge;
using Application.UseCases.Gamification.Commands.DeleteBadge;
using Application.UseCases.Gamification.Commands.UpdateBadge;

using Application.UseCases.Gamification.Commands.CreateBadgeRule;
using Application.UseCases.Gamification.Commands.UpdateBadgeRule;
using Application.UseCases.Gamification.Commands.DeleteBadgeRule;

using Application.UseCases.Gamification.EventHandlers;
using Application.UseCases.Gamification.Queries.GetLeaderboard;
using Application.UseCases.Gamification.Queries.GetMyBadgeProgress;
using Application.UseCases.Gamification.Queries.GetMyBadges;
using Application.UseCases.Gamification.Queries.GetMyGamification;
using Application.UseCases.Gamification.Queries.GetMyRewardHistory;
using Application.UseCases.Gamification.Queries.GetMyStreak;
using Application.UseCases.Gamification.Queries.GetBadgeRules;
using Application.UseCases.Gamification.Queries.GetContestRanking;
using Application.UseCases.Gamification.Queries.GetDailyActivities;


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
    // USER FEATURES
    // =====================================================

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyGamification(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetMyGamificationQuery(), ct));
    }

    [HttpGet("badges")]
    [Authorize]
    public async Task<IActionResult> GetMyBadges(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetMyBadgesQuery(), ct));
    }

    [HttpGet("badges/progress")]
    [Authorize]
    public async Task<IActionResult> GetBadgeProgress(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetMyBadgeProgressQuery(), ct));
    }

    [HttpGet("streak")]
    [Authorize]
    public async Task<IActionResult> GetMyStreak(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetMyStreakQuery(), ct));
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetRewardHistory(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetMyRewardHistoryQuery(), ct));
    }

    [HttpGet("daily-activities")]
    [Authorize]
    public async Task<IActionResult> GetDailyActivities(
        [FromQuery] int? year,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(
            new GetDailyActivitiesQuery { Year = year }, ct));
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
        type = type?.ToLower() ?? "exp";

        var allowedTypes = new[] { "exp", "solved", "streak", "badge" };

        if (!allowedTypes.Contains(type))
        {
            return BadRequest(new
            {
                error = "Invalid leaderboard type",
                allowed = allowedTypes
            });
        }

        var result = await _mediator.Send(
            new GetLeaderboardQuery { Type = type }, ct);

        return Ok(result);
    }

    // =====================================================
    // BADGE MANAGEMENT (ADMIN)
    // =====================================================

    [HttpPost("badges")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateBadge(
        [FromBody] CreateBadgeCommand command,
        CancellationToken ct)
    {
        var badgeId = await _mediator.Send(command, ct);

        return Created("", new
        {
            badgeId,
            message = "Badge created successfully"
        });
    }

    [HttpPut("badges/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateBadge(
        Guid id,
        [FromBody] UpdateBadgeCommand command,
        CancellationToken ct)
    {
        command.BadgeId = id;

        var success = await _mediator.Send(command, ct);

        if (!success)
            return NotFound(new { error = "Badge not found" });

        return Ok(new { message = "Badge updated successfully" });
    }

    [HttpDelete("badges/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteBadge(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(
            new DeleteBadgeCommand { BadgeId = id }, ct);

        if (!success)
            return NotFound(new { error = "Badge not found" });

        return Ok(new { message = "Badge deleted successfully" });
    }

    // =====================================================
    // 🔥 BADGE RULES (ADMIN)
    // =====================================================

    // CREATE RULE
    [HttpPost("badge-rules")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateBadgeRule(
        [FromBody] CreateBadgeRuleCommand command,
        CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);

        return Created("", new
        {
            ruleId = id,
            message = "Rule created successfully"
        });
    }

    // GET ALL RULES
    [HttpGet("badge-rules")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetBadgeRules(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBadgeRulesQuery(), ct);
        return Ok(result);
    }

    // UPDATE RULE
    [HttpPut("badge-rules/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateBadgeRule(
        Guid id,
        [FromBody] UpdateBadgeRuleCommand command,
        CancellationToken ct)
    {
        command.Id = id;

        var success = await _mediator.Send(command, ct);

        if (!success)
            return NotFound(new { error = "Rule not found" });

        return Ok(new { message = "Rule updated successfully" });
    }

    // DELETE (DISABLE) RULE
    [HttpDelete("badge-rules/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteBadgeRule(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(
            new DeleteBadgeRuleCommand { Id = id }, ct);

        if (!success)
            return NotFound(new { error = "Rule not found" });

        return Ok(new { message = "Rule disabled successfully" });
    }

    
}