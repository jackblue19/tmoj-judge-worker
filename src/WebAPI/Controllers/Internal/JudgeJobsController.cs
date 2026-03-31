using Contracts.Submissions.Judging;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.Internal;

[ApiController]
[Route("api/internal/judge/jobs")]
public sealed class JudgeJobsController : ControllerBase
{
    private readonly JudgeJobDispatchService _dispatchService;
    private readonly JudgeResultApplyService _resultApplyService;

    public JudgeJobsController(
        JudgeJobDispatchService dispatchService ,
        JudgeResultApplyService resultApplyService)
    {
        _dispatchService = dispatchService;
        _resultApplyService = resultApplyService;
    }

    [HttpPost("claim-next")]
    public async Task<IActionResult> ClaimNext([FromQuery] Guid workerId , CancellationToken ct)
    {
        var job = await _dispatchService.ClaimNextAsync(workerId , ct);
        if ( job is null )
            return NoContent();

        return Ok(job);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromBody] JudgeJobCompletedContract req ,
        CancellationToken ct)
    {
        await _resultApplyService.ApplyAsync(req , ct);
        return Ok(new { ok = true });
    }
}