using Contracts.Submissions.Judging;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Judging;

public sealed class JudgeResultApplyService
{
    private readonly TmojDbContext _db;

    public JudgeResultApplyService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task ApplyAsync(JudgeJobCompletedContract req , CancellationToken ct)
    {
        var submission = await _db.Submissions
            .FirstOrDefaultAsync(x => x.Id == req.SubmissionId , ct)
            ?? throw new InvalidOperationException($"Submission {req.SubmissionId} not found.");

        var judgeRun = await _db.JudgeRuns
            .FirstOrDefaultAsync(x => x.Id == req.JudgeRunId , ct)
            ?? throw new InvalidOperationException($"JudgeRun {req.JudgeRunId} not found.");

        var judgeJob = await _db.JudgeJobs
            .FirstOrDefaultAsync(x => x.Id == req.JobId , ct)
            ?? throw new InvalidOperationException($"JudgeJob {req.JobId} not found.");

        var normalizedJobStatus = req.Status == "done" ? "done" : "failed";

        judgeJob.Status = normalizedJobStatus;
        judgeJob.LastError = normalizedJobStatus == "done" ? null : req.Note;

        judgeRun.Status = normalizedJobStatus;
        judgeRun.FinishedAt = DateTime.UtcNow;
        judgeRun.Note = req.Note;
        judgeRun.CompileExitCode = req.Compile.ExitCode;
        judgeRun.CompileTimeMs = req.Compile.TimeMs;
        judgeRun.TotalTimeMs = req.Summary.TimeMs;
        judgeRun.TotalMemoryKb = req.Summary.MemoryKb;
        judgeRun.WorkerId = req.WorkerId;

        if ( !req.Compile.Ok )
        {
            submission.StatusCode = "done";
            submission.VerdictCode = "ce";
            submission.TimeMs = req.Summary.TimeMs;
            submission.MemoryKb = req.Summary.MemoryKb;
            submission.FinalScore = req.Summary.FinalScore ?? 0;
            submission.JudgedAt = DateTime.UtcNow;

            _db.Results.Add(new Result
            {
                Id = Guid.NewGuid() ,
                SubmissionId = submission.Id ,
                JudgeRunId = judgeRun.Id ,
                TestcaseId = null ,
                StatusCode = "ce" ,
                RuntimeMs = req.Summary.TimeMs ,
                MemoryKb = req.Summary.MemoryKb ,
                Input = null ,
                ExpectedOutput = null ,
                ActualOutput = null ,
                StdoutBlobId = null ,
                StderrBlobId = null ,
                CheckerMessage = null ,
                ExitCode = req.Compile.ExitCode ,
                Signal = null ,
                CreatedAt = DateTime.UtcNow ,
                OutputUrl = null ,
                Type = "compile" ,
                Message = "compile error" ,
                Note = $"{req.Compile.Stderr}\n{req.Compile.Stdout}"
            });

            await UpsertRunMetricAsync(submission.Id , req , ct);
            await _db.SaveChangesAsync(ct);
            return;
        }

        var resultEntities = req.Cases.Select(x => new Result
        {
            Id = Guid.NewGuid() ,
            SubmissionId = submission.Id ,
            JudgeRunId = judgeRun.Id ,
            TestcaseId = x.TestcaseId ,
            StatusCode = ResultStatusMapper.NormalizeVerdict(x.Verdict) ,
            RuntimeMs = x.TimeMs ,
            MemoryKb = x.MemoryKb ,
            Input = null ,
            ExpectedOutput = x.ExpectedOutput ,
            ActualOutput = x.ActualOutput ,
            StdoutBlobId = null ,
            StderrBlobId = null ,
            CheckerMessage = x.CheckerMessage ,
            ExitCode = x.ExitCode ,
            Signal = null ,
            CreatedAt = DateTime.UtcNow ,
            OutputUrl = null ,
            Type = "judge" ,
            Message = x.Message ,
            Note = x.Note
        }).ToList();

        _db.Results.AddRange(resultEntities);

        SubmissionFinalizer.ApplySubmissionSummary(submission , req.Summary);

        await UpsertRunMetricAsync(submission.Id , req , ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertRunMetricAsync(
        Guid submissionId ,
        JudgeJobCompletedContract req ,
        CancellationToken ct)
    {
        var metric = await _db.RunMetrics
            .FirstOrDefaultAsync(x => x.SubmissionId == submissionId , ct);

        if ( metric is null )
        {
            metric = new RunMetric
            {
                MetricId = Guid.NewGuid() ,
                SubmissionId = submissionId ,
                RuntimeMs = req.Summary.TimeMs ,
                MemoryKb = req.Summary.MemoryKb ,
                CpuUsage = null ,
                PassedTestcases = req.Summary.Passed ,
                TotalTestcases = req.Summary.Total ,
                CreatedAt = DateTime.UtcNow
            };

            _db.RunMetrics.Add(metric);
        }
        else
        {
            metric.RuntimeMs = req.Summary.TimeMs;
            metric.MemoryKb = req.Summary.MemoryKb;
            metric.PassedTestcases = req.Summary.Passed;
            metric.TotalTestcases = req.Summary.Total;
        }
    }
}