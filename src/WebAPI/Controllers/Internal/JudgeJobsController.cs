using Contracts.Submissions.Judging;
using Infrastructure.Configurations.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.Internal;

[ApiController]
[Route("api/internal/judge/jobs")]
public sealed class JudgeJobsController : ControllerBase
{
    private readonly JudgeJobDispatchService _dispatchService;
    private readonly JudgeResultApplyService _resultApplyService;
    private readonly IConfiguration _configuration;

    public JudgeJobsController(
        JudgeJobDispatchService dispatchService ,
        JudgeResultApplyService resultApplyService ,
        IConfiguration configuration)
    {
        _dispatchService = dispatchService;
        _resultApplyService = resultApplyService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("claim-next")]
    public async Task<IActionResult> ClaimNext(
        [FromQuery] Guid workerId ,
        CancellationToken ct)
    {
        var isInternal = InternalAuthHelper.IsInternalRequest(HttpContext);
        var hasApiKey = InternalAuthHelper.HasValidApiKey(HttpContext , _configuration);

        if ( !isInternal && !hasApiKey )
            return Unauthorized("Invalid internal access.");

        var job = await _dispatchService.ClaimNextAsync(workerId , ct);

        if ( job is null )
            return NoContent();

        return Ok(job);
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromBody] JudgeJobCompletedContract request ,
        CancellationToken ct)
    {
        var isInternal = InternalAuthHelper.IsInternalRequest(HttpContext);
        var hasApiKey = InternalAuthHelper.HasValidApiKey(HttpContext , _configuration);

        if ( !isInternal && !hasApiKey )
            return Unauthorized("Invalid internal access.");

        await _resultApplyService.ApplyAsync(request , ct);

        return Ok(new
        {
            succeeded = true ,
            code = "judge.job.completed" ,
            message = "Judge job completed."
        });
    }
}