using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/internal")]
public class InternalController : ControllerBase
{
    [HttpPost("judge-result")]
    public IActionResult Receive([FromBody] object data)
    {
        Console.WriteLine(data);
        return Ok("received");
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok("ok");
    }
}