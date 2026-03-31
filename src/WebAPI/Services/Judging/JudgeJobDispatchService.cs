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
        var worker = await _db.JudgeWorkers.FirstOrDefaultAsync(x => x.Id == workerId , ct);
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

        job.Status = "dequeued";
        job.DequeuedAt = DateTime.UtcNow;
        job.DequeuedByWorkerId = workerId;
        job.Attempts += 1;

        judgeRun.WorkerId = workerId;
        judgeRun.Status = "queued";

        submission.StatusCode = "judging";

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
        // TODO: sau này nếu source nằm blob/file store thì đọc từ đó.
        // Hiện tại bạn cần bảo đảm submit v2 đang lưu source ở nơi worker có thể đọc được.
        // Tạm thời fail fast để bạn không quên phần này.
        throw new InvalidOperationException(
            $"ResolveSourceCodeAsync is not implemented for Submission {submission.Id}. " +
            $"Bạn cần quyết định lưu source vào ArtifactBlob/StorageBlob hoặc cột riêng.");
    }
}