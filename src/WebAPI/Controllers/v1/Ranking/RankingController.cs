using Application.UseCases.Ranking.Dtos;
using Application.UseCases.Ranking.Queries.GetGlobalLeaderboard;
using Application.UseCases.Ranking.Queries.GetPublicContests;
using Application.UseCases.Ranking.Queries.GetPublicContestScoreboard;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Ranking;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class RankingController : ControllerBase
{
    private readonly IMediator _mediator;

    public RankingController(IMediator mediator) => _mediator = mediator;

    // ──────────────────────────────────────────
    // GET api/v1/ranking/global
    // ──────────────────────────────────────────
    [HttpGet("global")]
    public async Task<IActionResult> GetGlobalLeaderboard(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new GetGlobalLeaderboardQuery(page, pageSize, search), ct);

            return Ok(ApiResponse<GlobalLeaderboardDto>.Ok(result, "Global leaderboard fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the leaderboard." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/ranking/contests
    // ──────────────────────────────────────────
    [HttpGet("contests")]
    public async Task<IActionResult> GetPublicContests(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetPublicContestsQuery(), ct);

            return Ok(ApiResponse<List<PublicContestSummaryDto>>.Ok(result, "Public contests fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching contests." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/ranking/contests/{contestId}/scoreboard
    // ──────────────────────────────────────────
    [HttpGet("contests/{contestId:guid}/scoreboard")]
    public async Task<IActionResult> GetContestScoreboard(Guid contestId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GetPublicContestScoreboardQuery(contestId), ct);

            return Ok(ApiResponse<object>.Ok(result, "Contest scoreboard fetched successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the scoreboard." });
        }
    }
}
