using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Judging;

public sealed class SubmissionRejudgeService
{
    private readonly TmojDbContext _db;

    public SubmissionRejudgeService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> RejudgeAsync(
        Guid submissionId ,
        Guid? triggeredByUserId ,
        string? reason ,
        CancellationToken ct)
    {
        var submission = await _db.Submissions
            .FirstOrDefaultAsync(x => x.Id == submissionId , ct)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        submission.StatusCode = "queued";
        submission.VerdictCode = null;
        submission.FinalScore = null;
        submission.TimeMs = null;
        submission.MemoryKb = null;
        submission.JudgedAt = null;

        var judgeJob = new JudgeJob
        {
            Id = Guid.NewGuid() ,
            SubmissionId = submission.Id ,
            EnqueueAt = DateTime.UtcNow ,
            DequeuedByWorkerId = null ,
            DequeuedAt = null ,
            Status = "queued" ,
            Attempts = 0 ,
            LastError = null ,
            Priority = 0 ,
            TriggeredByUserId = triggeredByUserId ,
            TriggerType = "rejudge" ,
            TriggerReason = string.IsNullOrWhiteSpace(reason) ? "manual rejudge" : reason ,
            OptionsJson = null
        };

        _db.JudgeJobs.Add(judgeJob);

        await _db.SaveChangesAsync(ct);
        return judgeJob.Id;
    }
}