using Application.UseCases.Contests.Queries;
using Asp.Versioning;
using Infrastructure.Persistence.Scaffolded.Context;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Ranking;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class RankingController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly IMediator _mediator;

    public RankingController(TmojDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    // ──────────────────────────────────────────
    // GET api/v1/ranking/global
    // Global leaderboard ranked by solved public problems
    // ──────────────────────────────────────────
    [HttpGet("global")]
    public async Task<IActionResult> GetGlobalLeaderboard(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var statsQuery = _db.UserProblemStats
            .AsNoTracking()
            .Where(s => s.Solved
                     && s.Problem.VisibilityCode == "public"
                     && s.Problem.IsActive
                     && s.Problem.StatusCode == "published");

        var grouped = statsQuery
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Solved = g.Count(),
                TotalAttempts = g.Sum(x => x.Attempts),
            });

        var joined = grouped
            .Join(_db.Users,
                  g => g.UserId,
                  u => u.UserId,
                  (g, u) => new
                  {
                      g.UserId,
                      g.Solved,
                      g.TotalAttempts,
                      u.Username,
                      u.DisplayName,
                      u.FirstName,
                      u.LastName,
                      u.AvatarUrl,
                      u.RollNumber,
                      u.MemberCode,
                  });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            joined = joined.Where(x =>
                x.Username.ToLower().Contains(s) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(s)) ||
                (x.FirstName + " " + x.LastName).ToLower().Contains(s) ||
                (x.RollNumber != null && x.RollNumber.ToLower().Contains(s)) ||
                (x.MemberCode != null && x.MemberCode.ToLower().Contains(s)));
        }

        var ordered = joined
            .OrderByDescending(x => x.Solved)
            .ThenByDescending(x => x.TotalAttempts == 0
                ? 0
                : x.Solved * 100 / x.TotalAttempts);

        var total = await ordered.CountAsync(ct);

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ranked = rows.Select((x, i) => new
        {
            Rank = (page - 1) * pageSize + i + 1,
            UserId = x.UserId,
            Username = x.Username,
            Fullname = x.DisplayName ?? $"{x.FirstName} {x.LastName}".Trim(),
            AvatarUrl = x.AvatarUrl,
            RollNumber = x.RollNumber ?? x.MemberCode,
            Solved = x.Solved,
            TotalAttempts = x.TotalAttempts,
            Accuracy = x.TotalAttempts == 0
                ? 0
                : (int)Math.Round(x.Solved * 100.0 / x.TotalAttempts),
            Points = x.Solved * 10,
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Rows = ranked,
        }, "Global leaderboard fetched successfully"));
    }

    // ──────────────────────────────────────────
    // GET api/v1/ranking/contests
    // List all public published contests (for filter dropdown)
    // ──────────────────────────────────────────
    [HttpGet("contests")]
    public async Task<IActionResult> GetPublicContests(CancellationToken ct)
    {
        var contests = await _db.Contests
            .AsNoTracking()
            .Where(c => c.VisibilityCode == "public"
                     && c.Status == "published"
                     && c.IsActive)
            .OrderByDescending(c => c.StartAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Slug,
                c.StartAt,
                c.EndAt,
                c.Rules,
                Status = DateTime.UtcNow < c.StartAt ? "upcoming"
                       : DateTime.UtcNow > c.EndAt ? "ended"
                       : "running",
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(contests, "Public contests fetched successfully"));
    }

    // ──────────────────────────────────────────
    // GET api/v1/ranking/contests/{contestId}/scoreboard
    // Scoreboard for a specific public contest
    // ──────────────────────────────────────────
    [HttpGet("contests/{contestId:guid}/scoreboard")]
    public async Task<IActionResult> GetContestScoreboard(Guid contestId, CancellationToken ct)
    {
        var contest = await _db.Contests
            .AsNoTracking()
            .Where(c => c.Id == contestId
                     && c.VisibilityCode == "public"
                     && c.Status == "published"
                     && c.IsActive)
            .Select(c => new { c.Id })
            .FirstOrDefaultAsync(ct);

        if (contest is null)
            return NotFound(new { Message = "Contest not found or is not public." });

        var result = await _mediator.Send(
            new GetContestLeaderboardQuery { ContestId = contestId }, ct);

        return Ok(ApiResponse<object>.Ok(result, "Contest scoreboard fetched successfully"));
    }
}
