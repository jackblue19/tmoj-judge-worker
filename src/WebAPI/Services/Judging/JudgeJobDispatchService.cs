using Contracts.Submissions.Judging;
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

        var job = await _db.JudgeJobs
            .Where(x => x.Status == "queued")
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.EnqueueAt)
            .FirstOrDefaultAsync(ct);

        if ( job is null )
            return null;

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

        var judgeRun = await _db.JudgeRuns
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(x => x.SubmissionId == submission.Id , ct)
            ?? throw new InvalidOperationException($"JudgeRun for Submission {submission.Id} not found.");

        job.Status = "running";
        job.DequeuedAt = DateTime.UtcNow;
        job.DequeuedByWorkerId = workerId;
        job.Attempts += 1;

        judgeRun.WorkerId = workerId;
        judgeRun.Status = "running";

        submission.StatusCode = "running";

        await _db.SaveChangesAsync(ct);

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
            RuntimeImage = runtime.ImageRef ,
            TimeLimitMs = problem.TimeLimitMs ?? runtime.DefaultTimeLimitMs ,
            MemoryLimitKb = problem.MemoryLimitKb ?? runtime.DefaultMemoryLimitKb ,
            CompareMode = "trim" ,
            StopOnFirstFail = true ,
            SourceCode = await ResolveSourceCodeAsync(submission , ct) ,
            Cases = cases
        };
    }

    private Task<string> ResolveSourceCodeAsync(Domain.Entities.Submission submission , CancellationToken ct)
    {
        if ( !string.IsNullOrWhiteSpace(submission.SourceCode) )
            return Task.FromResult(submission.SourceCode);

        throw new InvalidOperationException(
            $"Submission {submission.Id} has no source_code. " +
            $"Need to load from code_artifact_id/storage_blob_id.");
    }
}