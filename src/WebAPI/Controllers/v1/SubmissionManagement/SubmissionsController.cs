using Asp.Versioning;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Judging;

namespace WebAPI.Controllers.v1.SubmissionManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class SubmissionsController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly LocalJudgeService _judge;

    public SubmissionsController(TmojDbContext db , LocalJudgeService judge)
    {
        _db = db;
        _judge = judge;
    }

    [HttpPost("run-sample")]
    public async Task<IActionResult> RunSample([FromRoute] Guid problemId , [FromBody] RunSampleRequest req , CancellationToken ct)
    {
        if ( req.RuntimeId == Guid.Empty )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "RuntimeId is required.");

        if ( string.IsNullOrWhiteSpace(req.SourceCode) )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "SourceCode is required.");

        if ( req.Tests is null || req.Tests.Count == 0 )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "Tests is required.");

        var runtime = await _db.Runtimes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.RuntimeId && x.IsActive , ct);

        if ( runtime is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Runtime not found or inactive.");

        var timeLimitMs = req.TimeLimitMs ?? runtime.DefaultTimeLimitMs;
        if ( timeLimitMs <= 0 )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "TimeLimitMs must be > 0.");

        var compareMode = req.CompareMode ?? CompareMode.Trim;

        var result = await _judge.CompileAndRunSamplesAsync(new CompileRunRequest
        {
            RuntimeId = runtime.Id ,
            RuntimeName = runtime.RuntimeName ,
            SourceCode = req.SourceCode ,
            TimeLimitMs = timeLimitMs ,
            CompareMode = compareMode ,
            Tests = req.Tests.Select((t , idx) => new SampleTest
            {
                Index = idx + 1 ,
                Input = t.Input ?? string.Empty ,
                ExpectedOutput = t.ExpectedOutput ?? string.Empty
            }).ToList()
        } , ct);

        return Ok(result);
    }
}
