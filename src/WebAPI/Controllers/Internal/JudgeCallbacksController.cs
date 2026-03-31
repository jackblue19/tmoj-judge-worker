using Contracts.Submissions.Judging;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.Internal;

[ApiController]
[Route("api/internal/judge/callbacks")]
public sealed class JudgeCallbacksController : ControllerBase
{
    private readonly JudgeResultApplyService _applyService;

    public JudgeCallbacksController(JudgeResultApplyService applyService)
    {
        _applyService = applyService;
    }

    [HttpPost("job-completed")]
    public async Task<IActionResult> JobCompleted(
        [FromBody] JudgeJobCompletedContract req ,
        CancellationToken ct)
    {
        await _applyService.ApplyAsync(req , ct);
        return Ok(new { ok = true });
    }
}