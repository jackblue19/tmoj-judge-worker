using Microsoft.AspNetCore.Mvc;
using WebAPI.Judging;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/debug/judges")]
public sealed class JudgeDebugController : ControllerBase
{
    private readonly JudgeConnectionRegistry _registry;

    public JudgeDebugController(JudgeConnectionRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet]
    public IActionResult GetOnlineJudges()
    {
        return Ok(new
        {
            onlineJudges = _registry.GetOnlineJudgeIds()
        });
    }
}