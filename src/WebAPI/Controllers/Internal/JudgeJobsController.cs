using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.Internal;

[ApiController]
[Route("api/internal/judge/jobs")]
public sealed class JudgeJobsController : ControllerBase
{
    private readonly JudgeJobDispatchService _dispatchService;

    public JudgeJobsController(JudgeJobDispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    [HttpPost("claim-next")]
    public async Task<IActionResult> ClaimNext([FromQuery] Guid workerId , CancellationToken ct)
    {
        var job = await _dispatchService.ClaimNextAsync(workerId , ct);
        if ( job is null )
            return NoContent();

        return Ok(job);
    }
}