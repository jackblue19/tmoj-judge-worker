using System.Data;
using System.Text.Json;
using Contracts.Submissions.Judging;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Judging;

public sealed class JudgeJobDispatchService
{
    private readonly TmojDbContext _db;

    public JudgeJobDispatchService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<DispatchJudgeJobContract?> ClaimNextAsync(Guid workerId , CancellationToken ct)
    {
        var worker = await _db.JudgeWorkers
            .FirstOrDefaultAsync(x => x.Id == workerId , ct);

        if ( worker is null )
            throw new InvalidOperationException($"JudgeWorker {workerId} not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted , ct);

        var job = await _db.JudgeJobs
            .FromSqlInterpolated($@"
                SELECT *
                FROM judge_jobs
                WHERE id = (
                    SELECT id
                    FROM judge_jobs
                    WHERE status = 'queued'
                    ORDER BY priority, enqueue_at
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )")
            .FirstOrDefaultAsync(ct);

        if ( job is null )
        {
            await tx.CommitAsync(ct);
            return null;
        }

        var submission = await _db.Submissions
            .FirstOrDefaultAsync(x => x.Id == job.SubmissionId , ct)
            ?? throw new InvalidOperationException($"Submission {job.SubmissionId} not found.");

        var problem = await _db.Problems
            .FirstOrDefaultAsync(x => x.Id == submission.ProblemId , ct)
            ?? throw new InvalidOperationException($"Problem {submission.ProblemId} not found.");

        var runtime = await _db.Runtimes
            .FirstOrDefaultAsync(x => x.Id == submission.RuntimeId , ct)
            ?? throw new InvalidOperationException($"Runtime {submission.RuntimeId} not found.");

        if ( submission.TestsetId is null )
            throw new InvalidOperationException($"Submission {submission.Id} has no TestsetId.");

        var testset = await _db.Testsets
            .FirstOrDefaultAsync(x => x.Id == submission.TestsetId.Value , ct)
            ?? throw new InvalidOperationException($"Testset {submission.TestsetId} not found.");

        var cases = await _db.Testcases
            .Where(x => x.TestsetId == testset.Id)
            .OrderBy(x => x.Ordinal)
            .Select(x => new DispatchJudgeCaseContract
            {
                TestcaseId = x.Id ,
                Ordinal = x.Ordinal ,
                Weight = x.Weight ,
                IsSample = x.IsSample
            })
            .ToListAsync(ct);

        var options = DeserializeOptions(job.OptionsJson , problem , runtime);

        job.Status = "running";
        job.DequeuedAt = DateTime.UtcNow;
        job.DequeuedByWorkerId = workerId;
        job.Attempts += 1;
        job.LastError = null;

        submission.StatusCode = "running";
        submission.VerdictCode = null;
        submission.FinalScore = null;
        submission.TimeMs = null;
        submission.MemoryKb = null;
        submission.JudgedAt = null;

        var resolvedTimeLimitMs = problem.TimeLimitMs ?? options.TimeLimitMs;
        var resolvedMemoryLimitKb = problem.MemoryLimitKb ?? options.MemoryLimitKb;

        var runtimeProfileKey = ResolveRuntimeProfileKey(runtime);
        var sourceFileName = ResolveSourceFileName(runtime);
        var compileCommand = runtime.CompileCommand?.Trim() ?? "";
        var runCommand = ResolveRunCommand(runtime);
        var hasCompileStep = !string.IsNullOrWhiteSpace(compileCommand);

        var judgeRun = new JudgeRun
        {
            Id = Guid.NewGuid() ,
            SubmissionId = submission.Id ,
            WorkerId = workerId ,
            StartedAt = DateTime.UtcNow ,
            FinishedAt = null ,
            Status = "running" ,
            RuntimeId = runtime.Id ,
            DockerImage = runtime.ImageRef ,
            Limits = JsonSerializer.Serialize(new
            {
                timeMs = resolvedTimeLimitMs ,
                memoryKb = resolvedMemoryLimitKb ,
                compareMode = options.CompareMode ,
                stopOnFirstFail = options.StopOnFirstFail ,
                profileKey = runtimeProfileKey
            }) ,
            Note = null ,
            CompileLogBlobId = null ,
            CompileExitCode = null ,
            CompileTimeMs = null ,
            TotalTimeMs = null ,
            TotalMemoryKb = null
        };

        _db.JudgeRuns.Add(judgeRun);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new DispatchJudgeJobContract
        {
            JobId = job.Id ,
            JudgeRunId = judgeRun.Id ,
            SubmissionId = submission.Id ,
            WorkerId = workerId ,
            ProblemId = problem.Id ,
            ProblemSlug = problem.Slug ?? throw new InvalidOperationException("Problem slug is null.") ,
            TestsetId = testset.Id ,
            RuntimeId = runtime.Id ,
            RuntimeName = runtime.RuntimeName ,
            RuntimeVersion = runtime.RuntimeVersion ,
            RuntimeProfileKey = runtimeProfileKey ,
            RuntimeImage = runtime.ImageRef ,
            SourceFileName = sourceFileName ,
            HasCompileStep = hasCompileStep ,
            CompileCommand = compileCommand ,
            RunCommand = runCommand ,
            TimeLimitMs = resolvedTimeLimitMs ,
            MemoryLimitKb = resolvedMemoryLimitKb ,
            CompareMode = options.CompareMode ,
            StopOnFirstFail = options.StopOnFirstFail ,
            SourceCode = await ResolveSourceCodeAsync(submission , ct) ,
            Cases = cases
        };
    }

    private static JudgeExecutionOptionsContract DeserializeOptions(
        string? optionsJson ,
        Problem problem ,
        Runtime runtime)
    {
        if ( !string.IsNullOrWhiteSpace(optionsJson) )
        {
            var parsed = JsonSerializer.Deserialize<JudgeExecutionOptionsContract>(optionsJson);
            if ( parsed is not null )
            {
                var timeLimitMs = problem.TimeLimitMs ?? (parsed.TimeLimitMs > 0 ? parsed.TimeLimitMs : runtime.DefaultTimeLimitMs);
                var memoryLimitKb = problem.MemoryLimitKb ?? (parsed.MemoryLimitKb > 0 ? parsed.MemoryLimitKb : runtime.DefaultMemoryLimitKb);

                return new JudgeExecutionOptionsContract
                {
                    TimeLimitMs = timeLimitMs ,
                    MemoryLimitKb = memoryLimitKb ,
                    CompareMode = string.IsNullOrWhiteSpace(parsed.CompareMode) ? "trim" : parsed.CompareMode ,
                    StopOnFirstFail = parsed.StopOnFirstFail
                };
            }
        }

        // Không có OptionsJson: mặc định IOI (chấm hết test case theo Weight).
        // Chỉ contest ACM mới bật StopOnFirstFail qua SubmitContestCommandHandler.
        return new JudgeExecutionOptionsContract
        {
            TimeLimitMs = problem.TimeLimitMs ?? runtime.DefaultTimeLimitMs ,
            MemoryLimitKb = problem.MemoryLimitKb ?? runtime.DefaultMemoryLimitKb ,
            CompareMode = "trim" ,
            StopOnFirstFail = false
        };
    }

    private static string ResolveRuntimeProfileKey(Runtime runtime)
    {
        if ( !string.IsNullOrWhiteSpace(runtime.ProfileKey) )
            return runtime.ProfileKey.Trim();

        throw new InvalidOperationException(
            $"Runtime '{runtime.RuntimeName}' (Id={runtime.Id}) has no profile_key configured.");
    }

    private static string ResolveSourceFileName(Runtime runtime)
    {
        if ( !string.IsNullOrWhiteSpace(runtime.SourceFileName) )
            return runtime.SourceFileName.Trim();

        throw new InvalidOperationException(
            $"Runtime '{runtime.RuntimeName}' (Id={runtime.Id}) has no source_file_name configured.");
    }

    private static string ResolveRunCommand(Runtime runtime)
    {
        if ( !string.IsNullOrWhiteSpace(runtime.RunCommand) )
            return runtime.RunCommand.Trim();

        throw new InvalidOperationException(
            $"Runtime '{runtime.RuntimeName}' (Id={runtime.Id}) has no run_command configured.");
    }

    private Task<string> ResolveSourceCodeAsync(Submission submission , CancellationToken ct)
    {
        if ( !string.IsNullOrWhiteSpace(submission.SourceCode) )
            return Task.FromResult(submission.SourceCode);

        throw new InvalidOperationException(
            $"Submission {submission.Id} has no source_code. " +
            $"Need to load from code_artifact_id/storage_blob_id.");
    }
}