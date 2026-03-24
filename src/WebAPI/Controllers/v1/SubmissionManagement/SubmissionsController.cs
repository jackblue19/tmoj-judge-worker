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

    public SubmissionsController(
        TmojDbContext db ,
        LocalJudgeService judge,
        IOptions<LocalStorageOptions> storage)
    {
        _db = db;
        _judge = judge;
        _storage = storage.Value;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Submit(
        Guid problemId ,
        [FromForm] SubmitFormDto req ,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var temp = "e1a709ec-dd49-4230-a5a2-6393be3b2578";

        if ( Guid.TryParse(temp , out Guid userGuid) )
        {
            // use userGuid here
            userId = userGuid;
            Console.WriteLine(userGuid);
        }
        else
        {
            Console.WriteLine("Invalid GUID format");
        }
        if ( userId == Guid.Empty )
            return Problem(statusCode: StatusCodes.Status401Unauthorized , title: "UserId not found in token.");

        if ( req.RuntimeId == Guid.Empty )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "RuntimeId is required.");

        var sourceCode = await ResolveSourceCode(req , ct);
        if ( string.IsNullOrWhiteSpace(sourceCode) )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "SourceCode or CodeFile is required.");

        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == problemId && x.IsActive , ct);
        if ( problem is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Problem not found.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "Problem.slug is required.");

        if ( req.RuntimeId == Guid.Empty )
            return Problem(statusCode: 400 , title: "RuntimeId is required.");

        var source = req.SourceCode;

        if ( string.IsNullOrWhiteSpace(source) && req.CodeFile is not null )
        {
            using var sr = new StreamReader(req.CodeFile.OpenReadStream() , Encoding.UTF8 , detectEncodingFromByteOrderMarks: true);
            source = await sr.ReadToEndAsync(ct);
        }

        if ( string.IsNullOrWhiteSpace(source) )
            return Problem(statusCode: 400 , title: "SourceCode or CodeFile is required.");

        var runtime = await _db.Runtimes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.RuntimeId && x.IsActive , ct);

        if ( runtime is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Runtime not found or inactive.");

        var testset = await _db.Testsets.AsNoTracking()
            .Where(x => x.ProblemId == problemId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if ( testset is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Active testset not found for this problem.");

        var testsetFolder = ResolveTestsetFolder(problem.Slug! , testset.Id);
        if ( !Directory.Exists(testsetFolder) )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Testset folder not found on disk.");

        var cases = await LoadJudgeCases(problemId , testset.Id , testsetFolder , ct);
        if ( cases.Count == 0 )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "No testcases found on disk for this testset.");

        var codeBytes = Encoding.UTF8.GetBytes(sourceCode);
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
        if ( timeLimitMs <= 0 )
            timeLimitMs = runtime.DefaultTimeLimitMs;

        var compareMode = req.CompareMode ?? CompareMode.Trim;
        var stopOnFirstFail = req.StopOnFirstFail ?? true;

        JudgeManyResponse judged;

        try
        {
            judged = await _judge.CompileAndRunManyAsync(new CompileRunManyRequest
            {
                //RuntimeName = runtime.RuntimeName ,
                RuntimeName = "C++ (g++)" ,
                SourceCode = source! ,
                TimeLimitMs = req.TimeLimitMs ?? 1000 ,
                CompareMode = req.CompareMode ?? CompareMode.Trim ,
                StopOnFirstFail = req.StopOnFirstFail ?? true ,
                Cases = cases
            } , ct);
        }
        catch ( Exception ex )
        {
            submission.StatusCode = "failed";
            submission.VerdictCode = "ie";
            submission.JudgedAt = DateTime.UtcNow;

            judgeRun.Status = "failed";
            judgeRun.FinishedAt = DateTime.UtcNow;
            judgeRun.Note = ex.Message;

            await _db.SaveChangesAsync(ct);

            return Problem(
                statusCode: StatusCodes.Status500InternalServerError ,
                title: "Judge failed." ,
                detail: ex.Message);
        }

        judgeRun.CompileExitCode = judged.Compile.ExitCode;
        judgeRun.TotalTimeMs = judged.Summary.TimeMs;
        judgeRun.Status = "done";
        judgeRun.FinishedAt = DateTime.UtcNow;

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
                Compile = MapCompile(judged.Compile) ,
                Summary = new SubmitSummary
                {
                    Passed = 0 ,
                    Total = cases.Count ,
                    TimeMs = judged.Summary.TimeMs
                } ,
                Failed = new List<SubmitFailedCase>()
            });
        }

        var resultEntities = judged.Cases.Select(c => new Result
        {
            Id = Guid.NewGuid() ,
            SubmissionId = submissionId ,
            JudgeRunId = judgeRunId ,
            TestcaseId = c.TestcaseId == Guid.Empty ? null : c.TestcaseId ,
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
                Message = x.Verdict == "wa" ? "Wrong Answer"
                      : x.Verdict == "tle" ? "Time Limit Exceeded"
                      : x.Verdict == "re" ? "Runtime Error"
                      : x.Verdict
            })
            .ToList();

        return Ok(new SubmitResponse
        {
            SubmissionId = submissionId ,
            StatusCode = submission.StatusCode ,
            VerdictCode = submission.VerdictCode ,
            Compile = new SubmitCompileDto
            {
                Ok = judged.Compile.Ok ,
                ExitCode = judged.Compile.ExitCode ,
                Stdout = judged.Compile.Stdout ,
                Stderr = judged.Compile.Stderr
            } ,
            Summary = new SubmitSummary
            {
                Passed = judged.Summary.Passed ,
                Total = cases.Count ,
                TimeMs = judged.Summary.TimeMs
            } ,
            Failed = failed
        });
    }

    //  helpers
    private Guid GetUserId()
    {
        var v =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("user_id") ??
            User.FindFirstValue("uid");

        return Guid.TryParse(v , out var id) ? id : Guid.Empty;
    }

    private async Task<string?> ResolveSourceCode(SubmitFormDto req , CancellationToken ct)
    {
        if ( !string.IsNullOrWhiteSpace(req.SourceCode) )
            return req.SourceCode;

        if ( req.CodeFile is null || req.CodeFile.Length == 0 )
            return null;

        await using var fs = req.CodeFile.OpenReadStream();
        using var sr = new StreamReader(fs , Encoding.UTF8 , detectEncodingFromByteOrderMarks: true);
        return await sr.ReadToEndAsync(ct);
    }

    private string ResolveTestsetFolder(string slug , Guid testsetId)
    {
        var root = _storage.ProblemsRoot;
        var slugSafe = SanitizeFolderName(slug);
        return Path.Combine(root , slugSafe , testsetId.ToString());
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Trim();
    }

    private static string? FindFirstExisting(string dir , params string[] candidates)
    {
        foreach ( var name in candidates )
        {
            var path = Path.Combine(dir , name);
            if ( System.IO.File.Exists(path) )
                return path;
        }
        return null;
    }

    private static async Task<string> ReadAllTextUtf8Async(string path , CancellationToken ct)
    {
        await using var fs = System.IO.File.OpenRead(path);
        using var sr = new StreamReader(fs , Encoding.UTF8 , detectEncodingFromByteOrderMarks: true);
        return await sr.ReadToEndAsync(ct);
    }

    private async Task<List<JudgeCaseInput>> LoadJudgeCases(
        Guid problemId ,
        Guid testsetId ,
        string testsetFolder ,
        CancellationToken ct)
    {
        var dbMap = await _db.Testcases.AsNoTracking()
            .Where(x => x.TestsetId == testsetId)
            .Select(x => new { x.Id , x.Ordinal })
            .ToDictionaryAsync(x => x.Ordinal , x => x.Id , ct);

        var cases = new List<JudgeCaseInput>();

        foreach ( var dir in Directory.EnumerateDirectories(testsetFolder) )
        {
            var folderName = Path.GetFileName(dir);
            if ( !int.TryParse(folderName , out var ordinalRaw) )
                continue;

            var inputPath = FindFirstExisting(dir , "input.inp" , "input.txt");
            var outputPath = FindFirstExisting(dir , "output.out" , "output.txt");

            if ( inputPath is null || outputPath is null )
                continue;

            var input = await ReadAllTextUtf8Async(inputPath , ct);
            var expected = await ReadAllTextUtf8Async(outputPath , ct);

            dbMap.TryGetValue(ordinalRaw , out var testcaseId);

            cases.Add(new JudgeCaseInput
            {
                TestcaseId = testcaseId ,
                Ordinal = ordinalRaw ,
                Input = input ,
                ExpectedOutput = expected
            });
        }

        return cases.OrderBy(x => x.Ordinal).ToList();
    }

    private static SubmitCompileDto MapCompile(CompileInfo x)
    {
        return new SubmitCompileDto
        {
            Ok = x.Ok ,
            ExitCode = x.ExitCode ,
            Stdout = x.Stdout ,
            Stderr = x.Stderr
        };
    }

    //  ABOVE => SUBMIT CODE
    //  GET ALL SUBMISSION BY USER ID
    [HttpGet("get-all")]
    public async Task<IActionResult> GetSubmissionsByUser(
        Guid problemId ,
        CancellationToken ct)
    {

        var userId = GetUserId();
        var temp = "e1a709ec-dd49-4230-a5a2-6393be3b2578";

        if ( Guid.TryParse(temp , out Guid userGuid) )
        {
            // use userGuid here
            userId = userGuid;
            Console.WriteLine(userGuid);
        }
        else
        {
            Console.WriteLine("Invalid GUID format");
        }

        /*var userId = GetUserId();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value?? 
        User.FindFirst("sub")?.Value;

        if ( userIdClaim == null )
            return Unauthorized("UserId not found in token");

        var userId = Guid.Parse(userIdClaim);
*/
        var submissions = await _db.Submissions.AsNoTracking()
            // .Where(x => x.ProblemId == problemId && x.UserId == userId && !x.IsDeleted)
            .Where(x => x.ProblemId == problemId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id ,
                x.ProblemId ,
                x.RuntimeId ,
                x.CodeSize ,
                x.CodeHash ,
                x.StatusCode ,
                x.VerdictCode ,
                x.FinalScore ,
                x.TimeMs ,
                x.MemoryKb ,
                x.JudgedAt ,
                x.TestsetId ,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(submissions);
    }
}