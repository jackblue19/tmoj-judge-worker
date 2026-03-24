using Microsoft.AspNetCore.Mvc;
using WebAPI.Judging;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/debug/submissions")]
public sealed class SubmissionDebugController : ControllerBase
{
    private readonly JudgeDispatchService _dispatchService;

    public SubmissionDebugController(JudgeDispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send(
        [FromBody] SubmissionRequestModel request ,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.DispatchSubmissionAsync(request , cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_dispatchService.GetAllSubmissions());
    }

    [HttpGet("{submissionId:int}")]
    public IActionResult GetById(int submissionId)
    {
        var submission = _dispatchService.GetSubmission(submissionId);
        if ( submission is null )
            return NotFound();

        return Ok(submission);
    }
}