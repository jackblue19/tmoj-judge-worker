using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.v2.SubmissionManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/submissions")]
[Authorize]
public sealed class SubmissionRejudgeController : ControllerBase
{
    private readonly SubmissionRejudgeService _rejudgeService;

    public SubmissionRejudgeController(SubmissionRejudgeService rejudgeService)
    {
        _rejudgeService = rejudgeService;
    }

    [HttpPost("{submissionId:guid}/rejudge")]
    public async Task<IActionResult> Rejudge(
        Guid submissionId ,
        [FromBody] RejudgeRequest? req ,
        CancellationToken ct)
    {
        if ( !User.IsInRole("admin") && !User.IsInRole("manager") && !User.IsInRole("teacher") )
            return Forbid();

        var actorUserId = GetUserIdNullable();

        var judgeJobId = await _rejudgeService.RejudgeAsync(
            submissionId ,
            actorUserId ,
            req?.Reason ,
            ct);

        return Ok(new
        {
            ok = true ,
            submissionId ,
            judgeJobId ,
            status = "queued"
        });
    }

    private Guid? GetUserIdNullable()
    {
        var v =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("user_id") ??
            User.FindFirstValue("uid");

        return Guid.TryParse(v , out var id) ? id : null;
    }
}

public sealed class RejudgeRequest
{
    public string? Reason { get; set; }
}