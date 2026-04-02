using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.Internal;

[ApiController]
[Route("api/internal/judge/metrics")]
public sealed class JudgeMetricsController : ControllerBase
{
    private readonly JudgeMetricsService _metricsService;

    public JudgeMetricsController(JudgeMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var overview = await _metricsService.GetOverviewAsync(ct);
        return Ok(overview);
    }
}