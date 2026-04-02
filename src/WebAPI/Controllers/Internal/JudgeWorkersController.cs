using Contracts.Submissions.Judging;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.Internal;

[ApiController]
[Route("api/internal/judge/workers")]
public sealed class JudgeWorkersController : ControllerBase
{
    private readonly JudgeWorkerHeartbeatService _heartbeatService;
    private readonly JudgeMetricsService _metricsService;

    public JudgeWorkersController(
        JudgeWorkerHeartbeatService heartbeatService ,
        JudgeMetricsService metricsService)
    {
        _heartbeatService = heartbeatService;
        _metricsService = metricsService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] JudgeWorkerRegistrationContract req ,
        CancellationToken ct)
    {
        var workerId = await _heartbeatService.RegisterAsync(req , ct);
        return Ok(new { workerId });
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(
        [FromBody] JudgeWorkerHeartbeatContract req ,
        CancellationToken ct)
    {
        await _heartbeatService.HeartbeatAsync(req , ct);
        return Ok(new { ok = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkers(CancellationToken ct)
    {
        var workers = await _metricsService.GetWorkersAsync(ct);
        return Ok(workers);
    }

    [HttpGet("{workerId:guid}")]
    public async Task<IActionResult> GetWorker(Guid workerId , CancellationToken ct)
    {
        var worker = await _metricsService.GetWorkerByIdAsync(workerId , ct);
        if ( worker is null )
            return NotFound();

        return Ok(worker);
    }
}