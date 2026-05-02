using Application.Common.Events;
using Contracts.Submissions.Judging;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Submissions;

namespace WebAPI.Services.Judging;

public sealed class JudgeResultApplyService
{
    private readonly TmojDbContext _db;
    private readonly SubmissionRealtimeNotifier _notifier;
    private readonly IMediator _mediator;

    public JudgeResultApplyService(
        TmojDbContext db ,
        SubmissionRealtimeNotifier notifier ,
        IMediator mediator)
    {
        _db = db;
        _notifier = notifier;
        _mediator = mediator;
    }

    public async Task ApplyAsync(
        JudgeJobCompletedContract req ,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var now = DateTime.UtcNow;

        var submission = await _db.Submissions
            .FirstOrDefaultAsync(x => x.Id == req.SubmissionId , ct)
            ?? throw new InvalidOperationException($"Submission {req.SubmissionId} not found.");

        var judgeRun = await _db.JudgeRuns
            .FirstOrDefaultAsync(x => x.Id == req.JudgeRunId , ct)
            ?? throw new InvalidOperationException($"JudgeRun {req.JudgeRunId} not found.");

        var judgeJob = await _db.JudgeJobs
            .FirstOrDefaultAsync(x => x.Id == req.JobId , ct)
            ?? throw new InvalidOperationException($"JudgeJob {req.JobId} not found.");

        if ( judgeJob.SubmissionId != submission.Id || judgeRun.SubmissionId != submission.Id )
            throw new InvalidOperationException("Judge job/run/submission mismatch.");

        var judgeJobStatus = JudgeStatusClassifier.NormalizeJudgeJobStatus(req);
        var judgeRunStatus = JudgeStatusClassifier.NormalizeJudgeRunStatus(req);
        var submissionStatus = JudgeStatusClassifier.NormalizeSubmissionStatus(req);

        var resolvedTimeMs = ResolveTotalTimeMs(req);
        var resolvedMemoryKb = ResolveTotalMemoryKb(req);

        judgeJob.Status = judgeJobStatus;
        judgeJob.LastError = judgeJobStatus == "failed"
            ? Truncate(FirstNotEmpty(req.Note , req.Compile.Stderr) , 2000)
            : null;

        judgeRun.Status = judgeRunStatus;
        judgeRun.FinishedAt = now;
        judgeRun.Note = BuildJudgeRunNote(req);
        judgeRun.CompileExitCode = req.Compile.ExitCode;
        judgeRun.CompileTimeMs = req.Compile.TimeMs;
        judgeRun.TotalTimeMs = resolvedTimeMs;
        judgeRun.TotalMemoryKb = resolvedMemoryKb;
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
            submission.TimeMs = resolvedTimeMs;
            submission.MemoryKb = resolvedMemoryKb;
            submission.FinalScore = req.Summary.FinalScore ?? 0;
            submission.JudgedAt = now;
            submission.Note = Truncate(FirstNotEmpty(req.Note , req.Compile.Stderr) , 4000);

            await UpsertRunMetricAsync(
                submission.Id ,
                req ,
                resolvedTimeMs ,
                resolvedMemoryKb ,
                ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await NotifyAsync(submission , ct);
            return;
        }

        if ( !req.Compile.Ok )
        {
            var compileVerdict = ResultStatusMapper.NormalizeVerdict(req.Summary.Verdict);

            submission.StatusCode = "done";
            submission.VerdictCode = compileVerdict;
            submission.TimeMs = resolvedTimeMs;
            submission.MemoryKb = resolvedMemoryKb;
            submission.FinalScore = req.Summary.FinalScore ?? 0;
            submission.JudgedAt = now;
            submission.Note = Truncate(
                FirstNotEmpty(req.Compile.Stderr , req.Compile.Stdout , req.Note) ,
                4000);

            _db.Results.Add(new Result
            {
                Id = Guid.NewGuid() ,
                SubmissionId = submission.Id ,
                JudgeRunId = judgeRun.Id ,
                TestcaseId = null ,
                StatusCode = compileVerdict ,
                RuntimeMs = resolvedTimeMs ,
                MemoryKb = resolvedMemoryKb ,
                Input = null ,
                ExpectedOutput = null ,
                ActualOutput = Truncate(req.Compile.Stdout , 8000) ,
                StdoutBlobId = null ,
                StderrBlobId = null ,
                CheckerMessage = BuildCompileCheckerMessage(compileVerdict) ,
                ExitCode = req.Compile.ExitCode ,
                Signal = null ,
                CreatedAt = now ,
                OutputUrl = null ,
                Type = "compile" ,
                Message = Truncate(
                    FirstNotEmpty(
                        BuildCompileMessage(compileVerdict) ,
                        req.Compile.Stderr ,
                        req.Note) ,
                    8000) ,
                Note = Truncate(
                    $"{req.Compile.Stderr}\n{req.Compile.Stdout}".Trim() ,
                    8000)
            });

            await UpsertRunMetricAsync(
                submission.Id ,
                req ,
                resolvedTimeMs ,
                resolvedMemoryKb ,
                ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await NotifyAsync(submission , ct);
            return;
        }

        var resultEntities = req.Cases
            .OrderBy(x => x.Ordinal)
            .Select(x => new Result
            {
                Id = Guid.NewGuid() ,
                SubmissionId = submission.Id ,
                JudgeRunId = judgeRun.Id ,
                TestcaseId = x.TestcaseId ,
                StatusCode = ResultStatusMapper.NormalizeVerdict(x.Verdict) ,
                RuntimeMs = x.TimeMs ,
                MemoryKb = x.MemoryKb ,
                Input = null ,
                ExpectedOutput = Truncate(x.ExpectedOutput , 8000) ,
                ActualOutput = Truncate(x.ActualOutput ?? x.Stdout , 8000) ,
                StdoutBlobId = null ,
                StderrBlobId = null ,
                CheckerMessage = Truncate(x.CheckerMessage , 4000) ,
                ExitCode = x.ExitCode ,
                Signal = null ,
                CreatedAt = now ,
                OutputUrl = null ,
                Type = "judge" ,
                Message = BuildCaseMessage(x) ,
                Note = Truncate(x.Note , 2000)
            })
            .ToList();

        _db.Results.AddRange(resultEntities);

        SubmissionFinalizer.ApplySubmissionSummary(
            submission ,
            req.Summary ,
            resolvedTimeMs ,
            resolvedMemoryKb);

        submission.StatusCode = "done";
        submission.JudgedAt ??= now;
        submission.Note = BuildSubmissionNote(req);

        if ( submission.VerdictCode == "ac" )
        {
            await _mediator.Publish(
                new ProblemSolvedEvent(
                    submission.UserId ,
                    submission.ProblemId ,
                    submission.Id) ,
                ct);
        }

        await UpsertRunMetricAsync(
            submission.Id ,
            req ,
            resolvedTimeMs ,
            resolvedMemoryKb ,
            ct);

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

    private async Task UpsertRunMetricAsync(
        Guid submissionId ,
        JudgeJobCompletedContract req ,
        int? resolvedTimeMs ,
        int? resolvedMemoryKb ,
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
                RuntimeMs = resolvedTimeMs ,
                MemoryKb = resolvedMemoryKb ,
                CpuUsage = null ,
                PassedTestcases = req.Summary.Passed ,
                TotalTestcases = req.Summary.Total ,
                CreatedAt = DateTime.UtcNow
            };

            _db.RunMetrics.Add(metric);
        }
        else
        {
            metric.RuntimeMs = resolvedTimeMs;
            metric.MemoryKb = resolvedMemoryKb;
            metric.PassedTestcases = req.Summary.Passed;
            metric.TotalTestcases = req.Summary.Total;
        }
    }

    private static int? ResolveTotalTimeMs(JudgeJobCompletedContract req)
    {
        if ( req.Summary.TimeMs.HasValue && req.Summary.TimeMs.Value > 0 )
            return req.Summary.TimeMs;

        var maxCaseTime = req.Cases
            .Where(x => x.TimeMs.HasValue)
            .Select(x => x.TimeMs!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return maxCaseTime > 0 ? maxCaseTime : null;
    }

    private static int? ResolveTotalMemoryKb(JudgeJobCompletedContract req)
    {
        if ( req.Summary.MemoryKb.HasValue && req.Summary.MemoryKb.Value > 0 )
            return req.Summary.MemoryKb;

        var maxCaseMemory = req.Cases
            .Where(x => x.MemoryKb.HasValue)
            .Select(x => x.MemoryKb!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return maxCaseMemory > 0 ? maxCaseMemory : null;
    }

    private static string BuildCompileCheckerMessage(string verdict)
    {
        verdict = ResultStatusMapper.NormalizeVerdict(verdict);

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
        verdict = ResultStatusMapper.NormalizeVerdict(verdict);

        return verdict switch
        {
            "ce" => "compile error",
            "tle" => "compile time limit exceeded",
            "mle" => "compile memory limit exceeded",
            _ => "compile failed"
        };
    }

    private static string? BuildJudgeRunNote(JudgeJobCompletedContract req)
    {
        var parts = new List<string>();

        if ( !string.IsNullOrWhiteSpace(req.Note) )
            parts.Add(req.Note);

        if ( !req.Compile.Ok || req.Compile.ExitCode != 0 )
        {
            if ( !string.IsNullOrWhiteSpace(req.Compile.Stdout) )
                parts.Add("compile stdout:\n" + req.Compile.Stdout);

            if ( !string.IsNullOrWhiteSpace(req.Compile.Stderr) )
                parts.Add("compile stderr:\n" + req.Compile.Stderr);
        }

        var text = string.Join("\n\n" , parts);
        return Truncate(text , 8000);
    }

    private static string? BuildSubmissionNote(JudgeJobCompletedContract req)
    {
        var finalVerdict = ResultStatusMapper.NormalizeVerdict(req.Summary.Verdict);

        if ( finalVerdict == "ce" )
        {
            return Truncate(
                FirstNotEmpty(req.Compile.Stderr , req.Compile.Stdout , req.Note) ,
                4000);
        }

        var firstFailed = req.Cases
            .OrderBy(x => x.Ordinal)
            .FirstOrDefault(x => ResultStatusMapper.NormalizeVerdict(x.Verdict) != "ac");

        if ( firstFailed is not null )
        {
            return Truncate(
                FirstNotEmpty(
                    firstFailed.Message ,
                    firstFailed.Stderr ,
                    firstFailed.CheckerMessage ,
                    firstFailed.Note ,
                    req.Note) ,
                4000);
        }

        return Truncate(req.Note , 4000);
    }

    private static string? BuildCaseMessage(JudgeCaseCompletedContract c)
    {
        var parts = new List<string>();

        if ( !string.IsNullOrWhiteSpace(c.Message) )
            parts.Add(c.Message);

        if ( !string.IsNullOrWhiteSpace(c.Stderr) )
            parts.Add(c.Stderr);

        if ( c.TimedOut )
            parts.Add("Process timed out.");

        var text = string.Join("\n" , parts);
        return Truncate(text , 8000);
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string? Truncate(string? value , int maxLength)
    {
        if ( string.IsNullOrWhiteSpace(value) )
            return null;

        value = value.Trim();

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}