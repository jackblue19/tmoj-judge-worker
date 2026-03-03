using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Extensions;
using WebAPI.Judging;

namespace WebAPI.Controllers.v1.SubmissionManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/problems/{problemId:guid}/submissions")]
public sealed class SubmissionsController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly LocalJudgeService _judge;
    private readonly LocalStorageOptions _storage;

    public SubmissionsController(TmojDbContext db , LocalJudgeService judge , IOptions<LocalStorageOptions> storage)
    {
        _db = db;
        _judge = judge;
        _storage = storage.Value;
    }

    // POST /api/v1/problems/{problemId}/submissions/run-sample
    [HttpPost("run-sample")]
    public async Task<IActionResult> RunSample(Guid problemId , [FromBody] RunSampleRequest req , CancellationToken ct)
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

    // POST /api/v1/problems/{problemId}/submissions
    [HttpPost]
    public async Task<IActionResult> Submit(Guid problemId , [FromBody] SubmitRequest req , CancellationToken ct)
    {
        var userId = GetUserId();
        if ( userId == Guid.Empty )
            return Problem(statusCode: StatusCodes.Status401Unauthorized , title: "UserId not found in token.");

        if ( req.RuntimeId == Guid.Empty )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "RuntimeId is required.");

        if ( string.IsNullOrWhiteSpace(req.SourceCode) )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "SourceCode is required.");

        var runtime = await _db.Runtimes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.RuntimeId && x.IsActive , ct);

        if ( runtime is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Runtime not found or inactive.");

        // active testset
        var testset = await _db.Testsets.AsNoTracking()
            .Where(x => x.ProblemId == problemId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if ( testset is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Active testset not found for this problem.");

        // resolve disk folder
        var (ok, testsetFolder, err) = await ResolveTestsetFolder(problemId , testset.Id , ct);
        if ( !ok ) return err!;

        // load cases from disk
        var cases = new List<JudgeCaseInput>();

        foreach ( var dir in Directory.EnumerateDirectories(testsetFolder) )
        {
            var folderName = Path.GetFileName(dir); // "001"
            if ( !int.TryParse(folderName , out var ordinalRaw) )
                continue;

            var inputPath = FindFirstExisting(dir , "input.inp" , "input.txt");
            var outputPath = FindFirstExisting(dir , "output.out" , "output.txt");

            if ( inputPath is null || outputPath is null )
                continue;

            var input = await ReadAllTextUtf8Async(inputPath , ct);
            var expected = await ReadAllTextUtf8Async(outputPath , ct);

            cases.Add(new JudgeCaseInput
            {
                TestcaseId = Guid.Empty , // phase B: disk-only
                Ordinal = ordinalRaw ,
                Input = input ,
                ExpectedOutput = expected
            });
        }

        cases = cases.OrderBy(x => x.Ordinal).ToList();

        if ( cases.Count == 0 )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "No testcases found on disk for this testset.");

        // create submission + judge_run first
        var codeBytes = Encoding.UTF8.GetBytes(req.SourceCode);
        var codeHash = Convert.ToHexString(SHA256.HashData(codeBytes)).ToLowerInvariant();

        var submissionId = Guid.NewGuid();
        var judgeRunId = Guid.NewGuid();

        var submission = new Submission
        {
            Id = submissionId ,
            UserId = userId ,
            ProblemId = problemId ,
            RuntimeId = runtime.Id ,
            CodeArtifactId = null ,
            CodeSize = codeBytes.Length ,
            CodeHash = codeHash ,
            StatusCode = "running" ,
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
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var judgeRun = new JudgeRun
        {
            Id = judgeRunId ,
            SubmissionId = submissionId ,
            WorkerId = null ,
            StartedAt = DateTime.UtcNow ,
            FinishedAt = null ,
            Status = "running" ,
            RuntimeId = runtime.Id ,
            DockerImage = null ,
            Limits = null ,
            Note = null ,
            CompileLogBlobId = null ,
            CompileExitCode = null ,
            CompileTimeMs = null ,
            TotalTimeMs = null ,
            TotalMemoryKb = null
        };

        _db.Submissions.Add(submission);
        _db.JudgeRuns.Add(judgeRun);
        await _db.SaveChangesAsync(ct);

        var timeLimitMs = req.TimeLimitMs ?? runtime.DefaultTimeLimitMs;
        var compareMode = req.CompareMode ?? CompareMode.Trim;
        var stopOnFirstFail = req.StopOnFirstFail ?? true;

        JudgeManyResponse judged;

        try
        {
            judged = await _judge.CompileAndRunManyAsync(new CompileRunManyRequest
            {
                RuntimeName = runtime.RuntimeName ,
                SourceCode = req.SourceCode ,
                TimeLimitMs = timeLimitMs ,
                CompareMode = compareMode ,
                StopOnFirstFail = stopOnFirstFail ,
                Cases = cases
            } , ct);
        }
        catch ( Exception ex )
        {
            submission.StatusCode = "done";
            submission.VerdictCode = "ie";
            submission.JudgedAt = DateTime.UtcNow;

            judgeRun.Status = "failed";
            judgeRun.FinishedAt = DateTime.UtcNow;
            judgeRun.Note = ex.Message;

            await _db.SaveChangesAsync(ct);

            return Problem(statusCode: StatusCodes.Status500InternalServerError , title: "Judge failed." , detail: ex.Message);
        }

        judgeRun.CompileExitCode = judged.Compile.ExitCode;
        judgeRun.TotalTimeMs = judged.Summary.TimeMs;
        judgeRun.Status = "done";
        judgeRun.FinishedAt = DateTime.UtcNow;

        // compile error
        if ( !judged.Compile.Ok )
        {
            submission.StatusCode = "done";
            submission.VerdictCode = "ce";
            submission.TimeMs = judged.Summary.TimeMs;
            submission.JudgedAt = DateTime.UtcNow;

            _db.Results.Add(new Result
            {
                Id = Guid.NewGuid() ,
                SubmissionId = submissionId ,
                JudgeRunId = judgeRunId ,
                TestcaseId = null ,
                StatusCode = "ce" ,
                RuntimeMs = judged.Summary.TimeMs ,
                MemoryKb = null ,
                Input = null ,
                ExpectedOutput = null ,
                ActualOutput = null ,
                StdoutBlobId = null ,
                StderrBlobId = null ,
                CheckerMessage = null ,
                ExitCode = judged.Compile.ExitCode ,
                Signal = null ,
                CreatedAt = DateTime.UtcNow ,
                OutputUrl = null ,
                Type = "compile" ,
                Message = "ce" ,
                Note = (judged.Compile.Stderr ?? "") + "\n" + (judged.Compile.Stdout ?? "")
            });

            await _db.SaveChangesAsync(ct);

            return Ok(new SubmitResponse
            {
                SubmissionId = submissionId ,
                StatusCode = submission.StatusCode ,
                VerdictCode = submission.VerdictCode ,
                Compile = judged.Compile ,
                Summary = new SubmitSummary
                {
                    Passed = 0 ,
                    Total = cases.Count ,
                    TimeMs = judged.Summary.TimeMs
                } ,
                Failed = new List<SubmitFailedCase>()
            });
        }

        // save per-case results
        var resultEntities = judged.Cases.Select(c => new Result
        {
            Id = Guid.NewGuid() ,
            SubmissionId = submissionId ,
            JudgeRunId = judgeRunId ,
            TestcaseId = null , // phase B: disk-only
            StatusCode = c.Verdict ,
            RuntimeMs = c.TimeMs ,
            MemoryKb = null ,
            Input = req.ReturnIO ? c.Input : null ,
            ExpectedOutput = req.ReturnIO ? c.ExpectedOutput : null ,
            ActualOutput = req.ReturnIO ? c.ActualOutput : null ,
            StdoutBlobId = null ,
            StderrBlobId = null ,
            CheckerMessage = c.Verdict == "wa" ? "Wrong Answer" : null ,
            ExitCode = c.ExitCode ,
            Signal = null ,
            CreatedAt = DateTime.UtcNow ,
            OutputUrl = null ,
            Type = "judge" ,
            Message = c.Verdict ,
            Note = null
        }).ToList();

        _db.Results.AddRange(resultEntities);

        submission.StatusCode = "done";
        submission.VerdictCode = judged.Summary.Verdict;
        submission.TimeMs = judged.Summary.TimeMs;
        submission.JudgedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var failed = judged.Cases
            .Where(x => x.Verdict != "ac")
            .Select(x => new SubmitFailedCase
            {
                Ordinal = x.Ordinal ,
                Verdict = x.Verdict ,
                Message = x.Verdict == "wa" ? "Wrong Answer" :
                          x.Verdict == "tle" ? "Time Limit Exceeded" :
                          x.Verdict == "re" ? "Runtime Error" :
                          x.Verdict
            })
            .ToList();

        return Ok(new SubmitResponse
        {
            SubmissionId = submissionId ,
            StatusCode = submission.StatusCode ,
            VerdictCode = submission.VerdictCode ,
            Compile = judged.Compile ,
            Summary = new SubmitSummary
            {
                Passed = judged.Summary.Passed ,
                Total = cases.Count ,
                TimeMs = judged.Summary.TimeMs
            } ,
            Failed = failed
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

    private async Task<(bool Ok, string TestsetFolder, IActionResult? Error)> ResolveTestsetFolder(Guid problemId , Guid testsetId , CancellationToken ct)
    {
        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == problemId , ct);
        if ( problem is null ) return (false, "", NotFound("Problem not found."));

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            return (false, "", BadRequest("Problem.slug is required."));

        var testsetExists = await _db.Testsets.AsNoTracking()
            .AnyAsync(x => x.Id == testsetId && x.ProblemId == problemId , ct);

        if ( !testsetExists ) return (false, "", NotFound("Testset not found."));

        var root = _storage.ProblemsRoot;
        if ( string.IsNullOrWhiteSpace(root) )
            return (false, "", Problem("LocalStorage.ProblemsRoot is not configured."));

        var slugSafe = StoragePathHelper.SanitizeFolderName(problem.Slug);
        var testsetFolder = Path.Combine(root , slugSafe , testsetId.ToString());

        if ( !Directory.Exists(testsetFolder) )
            return (false, "", NotFound("Testset folder not found on disk."));

        return (true, testsetFolder, null);
    }

    private static string? FindFirstExisting(string folder , params string[] names)
    {
        foreach ( var n in names )
        {
            var p = Path.Combine(folder , n);
            if ( System.IO.File.Exists(p) ) return p;
        }
        return null;
    }

    private static async Task<string> ReadAllTextUtf8Async(string path , CancellationToken ct)
    {
        using var fs = System.IO.File.OpenRead(path);
        using var sr = new StreamReader(fs , Encoding.UTF8 , detectEncodingFromByteOrderMarks: true);
        return await sr.ReadToEndAsync(ct);
    }
}