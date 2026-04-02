using Asp.Versioning;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Models.Submissions;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.v2.SubmissionManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/submissions")]
[Authorize]
public sealed class SubmissionQueriesController : ControllerBase
{
    private readonly SubmissionQueryService _queryService;
    private readonly TmojDbContext _db;

    public SubmissionQueriesController(
        SubmissionQueryService queryService ,
        TmojDbContext db)
    {
        _queryService = queryService;
        _db = db;
    }

    [HttpGet("{submissionId:guid}")]
    public async Task<IActionResult> GetDetail(
        Guid submissionId ,
        CancellationToken ct)
    {
        var detail = await _queryService.GetDetailAsync(submissionId , ct);
        if ( detail is null )
            return NotFound();

        var currentUserId = GetUserId();
        var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");

        if ( detail.UserId != currentUserId && !isAdmin )
            return Forbid();

        return Ok(detail);
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] SubmissionSearchRequest req ,
        CancellationToken ct)
    {
        var currentUserId = GetUserId();
        var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");

        if ( !isAdmin )
            req.UserId = currentUserId;

        var result = await _queryService.SearchAsync(req , ct);
        return Ok(result);
    }

    [HttpGet("my/latest")]
    public async Task<IActionResult> GetMyLatestByProblem(
        [FromQuery] Guid problemId ,
        CancellationToken ct)
    {
        if ( problemId == Guid.Empty )
            return BadRequest(new { error = "problemId is required." });

        var currentUserId = GetUserId();

        var latestSubmissionId = await _db.Submissions
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId && x.ProblemId == problemId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if ( latestSubmissionId == Guid.Empty )
            return NotFound();

        var detail = await _queryService.GetDetailAsync(latestSubmissionId , ct);
        return Ok(detail);
    }

    private Guid GetUserId()
    {
        var v =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("user_id") ??
            User.FindFirstValue("uid");

        return Guid.TryParse(v , out var id) ? id : Guid.Empty;
    }
}