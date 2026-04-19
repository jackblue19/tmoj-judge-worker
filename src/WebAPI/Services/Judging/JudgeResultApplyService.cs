using Application.Common.Events;
using Contracts.Submissions.Judging;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Submissions;
using MediatR;
using Application.UseCases.Gamification.EventHandlers;
namespace WebAPI.Services.Judging;

public sealed class JudgeResultApplyService
{
    private readonly TmojDbContext _db;
    private readonly SubmissionRealtimeNotifier _notifier;
    private readonly IMediator _mediator;

    public JudgeResultApplyService(
        TmojDbContext db ,
        SubmissionRealtimeNotifier notifier,
        IMediator mediator)

    {
        _db = db;
        _notifier = notifier;
        _mediator = mediator;
    }

    public async Task ApplyAsync(JudgeJobCompletedContract req , CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var submission = await _db.Submissions
            .FirstOrDefaultAsync(x => x.Id == req.SubmissionId , ct)
            ?? throw new InvalidOperationException($"Submission {req.SubmissionId} not found.");

        var judgeRun = await _db.JudgeRuns
            .FirstOrDefaultAsync(x => x.Id == req.JudgeRunId , ct)
            ?? throw new InvalidOperationException($"JudgeRun {req.JudgeRunId} not found.");

        var judgeJob = await _db.JudgeJobs
            .FirstOrDefaultAsync(x => x.Id == req.JobId , ct)
            ?? throw new InvalidOperationException($"JudgeJob {req.JobId} not found.");

        var judgeJobStatus = JudgeStatusClassifier.NormalizeJudgeJobStatus(req);
        var judgeRunStatus = JudgeStatusClassifier.NormalizeJudgeRunStatus(req);
        var submissionStatus = JudgeStatusClassifier.NormalizeSubmissionStatus(req);

        judgeJob.Status = judgeJobStatus;
        judgeJob.LastError = judgeJobStatus == "failed" ? req.Note : null;

        judgeRun.Status = judgeRunStatus;
        judgeRun.FinishedAt = DateTime.UtcNow;
        judgeRun.Note = req.Note;
        judgeRun.CompileExitCode = req.Compile.ExitCode;
        judgeRun.CompileTimeMs = req.Compile.TimeMs;
        judgeRun.TotalTimeMs = req.Summary.TimeMs;
        judgeRun.TotalMemoryKb = req.Summary.MemoryKb;
        judgeRun.WorkerId = req.WorkerId;

        submission.StatusCode = submissionStatus;

        var oldResults = await _db.Results
            .Where(x => x.JudgeRunId == judgeRun.Id)
            .ToListAsync(ct);

        if ( oldResults.Count > 0 )
            _db.Results.RemoveRange(oldResults);

        if ( JudgeStatusClassifier.IsInfrastructureFailure(req) )
        {
            submission.VerdictCode = "ie";
            submission.TimeMs = req.Summary.TimeMs;
            submission.MemoryKb = req.Summary.MemoryKb;
            submission.FinalScore = req.Summary.FinalScore ?? 0;
            submission.JudgedAt = DateTime.UtcNow;

            await UpsertRunMetricAsync(submission.Id , req , ct);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await NotifyAsync(submission , ct);
            return;
        }

        if (!req.Compile.Ok)
        {
            submission.StatusCode = "done";
            submission.VerdictCode = ResultStatusMapper.NormalizeVerdict(req.Summary.Verdict);
            submission.TimeMs = req.Summary.TimeMs;
            submission.MemoryKb = req.Summary.MemoryKb;
            submission.FinalScore = req.Summary.FinalScore ?? 0;
            submission.JudgedAt = DateTime.UtcNow;

            _db.Results.Add(new Result
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                JudgeRunId = judgeRun.Id,
                TestcaseId = null,
                StatusCode = ResultStatusMapper.NormalizeVerdict(req.Summary.Verdict),
                RuntimeMs = req.Summary.TimeMs,
                MemoryKb = req.Summary.MemoryKb,
                Input = null,
                ExpectedOutput = null,
                ActualOutput = null,
                StdoutBlobId = null,
                StderrBlobId = null,
                CheckerMessage = BuildCompileCheckerMessage(req.Summary.Verdict),
                ExitCode = req.Compile.ExitCode,
                Signal = null,
                CreatedAt = DateTime.UtcNow,
                OutputUrl = null,
                Type = "compile",
                Message = BuildCompileMessage(req.Summary.Verdict),
                Note = $"{req.Compile.Stderr}\n{req.Compile.Stdout}".Trim()
            });

            await UpsertRunMetricAsync(submission.Id, req, ct);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await NotifyAsync(submission, ct);


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

        submission.StatusCode = "done";
        submission.JudgedAt ??= DateTime.UtcNow;

        // =========================
        // TRIGGER PROBLEM SOLVED EVENT (AC only)
        // =========================
        if (submission.VerdictCode == "ac")
        {
            await _mediator.Publish(
                new ProblemSolvedEvent(
                    submission.UserId,
                    submission.ProblemId,
                    submission.Id // ✅ ADD THIS
                ),
                ct);
        }
        await UpsertRunMetricAsync(submission.Id , req , ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await NotifyAsync(submission , ct);
    }

    private async Task NotifyAsync(
        Submission submission ,
        CancellationToken ct)
    {
        await _notifier.NotifySubmissionCompletedAsync(
            new SubmissionVerdictEventDto
            {
                SubmissionId = submission.Id ,
                UserId = submission.UserId ,
                StatusCode = submission.StatusCode ,
                VerdictCode = submission.VerdictCode ,
                FinalScore = submission.FinalScore ,
                TimeMs = submission.TimeMs ,
                MemoryKb = submission.MemoryKb ,
                JudgedAt = submission.JudgedAt
            } ,
            ct);
    }

    private static string BuildCompileCheckerMessage(string verdict)
    {
        return verdict switch
        {
            "ce" => "Compile Error",
            "tle" => "Compile Time Limit Exceeded",
            "mle" => "Compile Memory Limit Exceeded",
            _ => "Compile Failed"
        };
    }

    private static string BuildCompileMessage(string verdict)
    {
        return verdict switch
        {
            "ce" => "compile error",
            "tle" => "compile time limit exceeded",
            "mle" => "compile memory limit exceeded",
            _ => "compile failed"
        };
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