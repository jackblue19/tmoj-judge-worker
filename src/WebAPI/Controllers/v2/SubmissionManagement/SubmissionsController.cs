using Asp.Versioning;
using Contracts.Submissions.Judging;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebAPI.Controllers.v2.SubmissionManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/problems/{problemId:guid}/submissions")]
[Authorize]
public sealed class SubmissionsController : ControllerBase
{
    private readonly TmojDbContext _db;

    public SubmissionsController(TmojDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1_000_000)]
    public async Task<IActionResult> Submit(
        Guid problemId ,
        [FromForm] SubmitRequestV2 req ,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if ( userId == Guid.Empty )
            return Problem(statusCode: 401 , title: "UserId not found in token.");

        if ( req.RuntimeId == Guid.Empty )
            return Problem(statusCode: 400 , title: "RuntimeId is required.");

        var sourceCode = await ResolveSourceCodeAsync(req , ct);
        if ( string.IsNullOrWhiteSpace(sourceCode) )
            return Problem(statusCode: 400 , title: "SourceCode or CodeFile is required.");

        var problem = await _db.Problems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == problemId && x.IsActive , ct);

        if ( problem is null )
            return Problem(statusCode: 404 , title: "Problem not found.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            return Problem(statusCode: 400 , title: "Problem slug is required.");

        var runtime = await _db.Runtimes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.RuntimeId && x.IsActive , ct);

        if ( runtime is null )
            return Problem(statusCode: 404 , title: "Runtime not found or inactive.");

        var testset = await _db.Testsets
            .AsNoTracking()
            .Where(x => x.ProblemId == problemId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if ( testset is null )
            return Problem(statusCode: 404 , title: "Active testset not found.");

        var hasAnyCase = await _db.Testcases
            .AsNoTracking()
            .AnyAsync(x => x.TestsetId == testset.Id , ct);

        if ( !hasAnyCase )
            return Problem(statusCode: 400 , title: "Testset has no testcases.");

        var codeBytes = Encoding.UTF8.GetBytes(sourceCode);
        var codeHash = Convert.ToHexString(SHA256.HashData(codeBytes)).ToLowerInvariant();

        var submissionId = Guid.NewGuid();
        var judgeJobId = Guid.NewGuid();

        var timeLimitMs = problem.TimeLimitMs ?? runtime.DefaultTimeLimitMs;
        if ( timeLimitMs <= 0 )
            timeLimitMs = runtime.DefaultTimeLimitMs;

        var memoryLimitKb = problem.MemoryLimitKb ?? runtime.DefaultMemoryLimitKb;
        if ( memoryLimitKb <= 0 )
            memoryLimitKb = runtime.DefaultMemoryLimitKb;

        var executionOptions = new JudgeExecutionOptionsContract
        {
            TimeLimitMs = timeLimitMs ,
            MemoryLimitKb = memoryLimitKb ,
            CompareMode = string.IsNullOrWhiteSpace(req.CompareMode) ? "trim" : req.CompareMode!.Trim().ToLowerInvariant() ,
            StopOnFirstFail = req.StopOnFirstFail ?? true
        };

        var submission = new Submission
        {
            Id = submissionId ,
            UserId = userId ,
            ProblemId = problemId ,
            RuntimeId = runtime.Id ,
            CodeArtifactId = null ,
            CodeSize = codeBytes.Length ,
            CodeHash = codeHash ,
            StatusCode = "queued" ,
            VerdictCode = null ,
            FinalScore = null ,
            TimeMs = null ,
            MemoryKb = null ,
            JudgedAt = null ,
            TestsetId = testset.Id ,
            IsDeleted = false ,
            CreatedAt = DateTime.UtcNow ,
            TeamId = null ,
            ContestProblemId = null ,
            TestcaseId = null ,
            CustomInput = null ,
            Type = "practice" ,
            StorageBlobId = null ,
            SubmissionType = "practice" ,
            IpAddress = null ,
            UserAgent = Request.Headers.UserAgent.ToString() ,
            SourceCode = sourceCode
        };

        var judgeJob = new JudgeJob
        {
            Id = judgeJobId ,
            SubmissionId = submissionId ,
            EnqueueAt = DateTime.UtcNow ,
            DequeuedByWorkerId = null ,
            DequeuedAt = null ,
            Status = "queued" ,
            Attempts = 0 ,
            LastError = null ,
            Priority = 0 ,
            TriggeredByUserId = userId ,
            TriggerType = "submit" ,
            TriggerReason = null ,
            OptionsJson = JsonSerializer.Serialize(executionOptions)
        };

        try
        {
            _db.Submissions.Add(submission);
            _db.JudgeJobs.Add(judgeJob);

            await _db.SaveChangesAsync(ct);
        }
        catch ( DbUpdateException ex )
        {
            return Problem(
                statusCode: 500 ,
                title: "DbUpdateException" ,
                detail: ex.InnerException?.Message ?? ex.Message);
        }
        catch ( Exception ex )
        {
            return Problem(
                statusCode: 500 ,
                title: "Unhandled exception" ,
                detail: ex.Message);
        }

        return Ok(new SubmitResponseV2
        {
            SubmissionId = submissionId ,
            JudgeRunId = null ,
            JudgeJobId = judgeJobId ,
            Status = "queued"
        });
    }

    private Guid GetUserId()
    {
        var v =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("user_id") ??
            User.FindFirstValue("uid");

        return Guid.TryParse(v , out var id) ? id : Guid.Empty;
    }

    private static async Task<string?> ResolveSourceCodeAsync(SubmitRequestV2 req , CancellationToken ct)
    {
        if ( !string.IsNullOrWhiteSpace(req.SourceCode) )
            return req.SourceCode;

        if ( req.CodeFile is null || req.CodeFile.Length == 0 )
            return null;

        await using var fs = req.CodeFile.OpenReadStream();
        using var sr = new StreamReader(fs , Encoding.UTF8 , detectEncodingFromByteOrderMarks: true);
        return await sr.ReadToEndAsync(ct);
    }
}

public sealed class SubmitRequestV2
{
    public Guid RuntimeId { get; set; }
    public string? SourceCode { get; set; }

    //[FromForm(Name = "file")]
    public IFormFile? CodeFile { get; set; }

    public string? CompareMode { get; set; }
    public bool? StopOnFirstFail { get; set; }
}

public sealed class SubmitResponseV2
{
    public Guid SubmissionId { get; set; }
    public Guid? JudgeRunId { get; set; }
    public Guid JudgeJobId { get; set; }
    public string Status { get; set; } = null!;
}