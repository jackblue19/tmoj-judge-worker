using Contracts.Submissions.Judging;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Judging;

public sealed class JudgeJobCompleteService
{
    private readonly TmojDbContext _db;

    public JudgeJobCompleteService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task CompleteAsync(
        JudgeJobCompletedContract request ,
        CancellationToken ct)
    {
        var job = await _db.JudgeJobs
            .FirstOrDefaultAsync(x => x.Id == request.JobId , ct)
            ?? throw new KeyNotFoundException("Judge job not found.");

        var submission = await _db.Submissions
            .FirstOrDefaultAsync(x => x.Id == request.SubmissionId , ct)
            ?? throw new KeyNotFoundException("Submission not found.");

        var judgeRun = await _db.JudgeRuns
            .FirstOrDefaultAsync(x => x.Id == request.JudgeRunId , ct)
            ?? throw new KeyNotFoundException("Judge run not found.");

        if ( job.SubmissionId != submission.Id || judgeRun.SubmissionId != submission.Id )
            throw new InvalidOperationException("Judge job/run/submission mismatch.");

        var now = DateTime.UtcNow;

        var normalizedStatus = NormalizeStatus(request.Status);
        var verdict = NormalizeVerdict(request.Summary.Verdict);

        var totalTimeMs = request.Summary.TimeMs
            ?? request.Cases
                .Where(x => x.TimeMs.HasValue)
                .Select(x => x.TimeMs!.Value)
                .DefaultIfEmpty(0)
                .Max();

        var totalMemoryKb = request.Summary.MemoryKb
            ?? request.Cases
                .Where(x => x.MemoryKb.HasValue)
                .Select(x => x.MemoryKb!.Value)
                .DefaultIfEmpty(0)
                .Max();

        job.Status = normalizedStatus;
        job.LastError = normalizedStatus == "failed"
            ? Truncate(request.Note ?? request.Compile.Stderr , 2000)
            : null;

        judgeRun.Status = normalizedStatus;
        judgeRun.FinishedAt = now;
        judgeRun.CompileExitCode = request.Compile.ExitCode;
        judgeRun.CompileTimeMs = request.Compile.TimeMs;
        judgeRun.TotalTimeMs = totalTimeMs > 0 ? totalTimeMs : null;
        judgeRun.TotalMemoryKb = totalMemoryKb > 0 ? totalMemoryKb : null;
        judgeRun.Note = BuildJudgeRunNote(request);

        submission.StatusCode = normalizedStatus;
        submission.VerdictCode = verdict;
        submission.FinalScore = request.Summary.FinalScore;
        submission.TimeMs = totalTimeMs > 0 ? totalTimeMs : null;
        submission.MemoryKb = totalMemoryKb > 0 ? totalMemoryKb : null;
        submission.JudgedAt = now;
        submission.Note = BuildSubmissionNote(verdict , request);

        await RemoveOldResultsAsync(submission.Id , judgeRun.Id , ct);

        if ( verdict == "ce" && request.Cases.Count == 0 )
        {
            _db.Results.Add(new Result
            {
                Id = Guid.NewGuid() ,
                SubmissionId = submission.Id ,
                JudgeRunId = judgeRun.Id ,
                TestcaseId = null ,
                StatusCode = "ce" ,
                RuntimeMs = request.Compile.TimeMs ,
                MemoryKb = null ,
                Input = null ,
                ExpectedOutput = null ,
                ActualOutput = Truncate(request.Compile.Stdout , 8000) ,
                StdoutBlobId = null ,
                StderrBlobId = null ,
                CheckerMessage = null ,
                ExitCode = request.Compile.ExitCode ,
                Signal = null ,
                CreatedAt = now ,
                OutputUrl = null ,
                Type = "compile" ,
                Message = Truncate(request.Compile.Stderr , 8000) ,
                Note = Truncate(request.Note , 2000)
            });
        }
        else
        {
            foreach ( var c in request.Cases.OrderBy(x => x.Ordinal) )
            {
                var caseVerdict = NormalizeVerdict(c.Verdict);

                _db.Results.Add(new Result
                {
                    Id = Guid.NewGuid() ,
                    SubmissionId = submission.Id ,
                    JudgeRunId = judgeRun.Id ,
                    TestcaseId = c.TestcaseId ,
                    StatusCode = caseVerdict ,
                    RuntimeMs = c.TimeMs ,
                    MemoryKb = c.MemoryKb ,
                    Input = null ,
                    ExpectedOutput = Truncate(c.ExpectedOutput , 8000) ,
                    ActualOutput = Truncate(c.ActualOutput ?? c.Stdout , 8000) ,
                    StdoutBlobId = null ,
                    StderrBlobId = null ,
                    CheckerMessage = Truncate(c.CheckerMessage , 4000) ,
                    ExitCode = c.ExitCode ,
                    Signal = null ,
                    CreatedAt = now ,
                    OutputUrl = null ,
                    Type = null ,
                    Message = BuildCaseMessage(c) ,
                    Note = Truncate(c.Note , 2000)
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task RemoveOldResultsAsync(
        Guid submissionId ,
        Guid judgeRunId ,
        CancellationToken ct)
    {
        var oldResults = await _db.Results
            .Where(x => x.SubmissionId == submissionId && x.JudgeRunId == judgeRunId)
            .ToListAsync(ct);

        if ( oldResults.Count > 0 )
            _db.Results.RemoveRange(oldResults);
    }

    private static string NormalizeStatus(string? status)
    {
        var value = Normalize(status);

        return value switch
        {
            "done" => "done",
            "failed" => "failed",
            _ => "failed"
        };
    }

    private static string NormalizeVerdict(string? verdict)
    {
        var value = Normalize(verdict);

        return value switch
        {
            "accepted" => "ac",
            "wrong_answer" => "wa",
            "time_limit_exceeded" => "tle",
            "memory_limit_exceeded" => "mle",
            "compile_error" => "ce",
            "runtime_error" => "re",

            "ac" => "ac",
            "wa" => "wa",
            "tle" => "tle",
            "mle" => "mle",
            "ce" => "ce",
            "re" => "re",
            "ie" => "ie",

            _ => string.IsNullOrWhiteSpace(value) ? "ie" : value
        };
    }

    private static string? BuildJudgeRunNote(JudgeJobCompletedContract request)
    {
        var parts = new List<string>();

        if ( !string.IsNullOrWhiteSpace(request.Note) )
            parts.Add(request.Note);

        if ( !request.Compile.Ok || request.Compile.ExitCode != 0 )
        {
            if ( !string.IsNullOrWhiteSpace(request.Compile.Stdout) )
                parts.Add("compile stdout:\n" + request.Compile.Stdout);

            if ( !string.IsNullOrWhiteSpace(request.Compile.Stderr) )
                parts.Add("compile stderr:\n" + request.Compile.Stderr);
        }

        var text = string.Join("\n\n" , parts);
        return Truncate(text , 8000);
    }

    private static string? BuildSubmissionNote(
        string verdict ,
        JudgeJobCompletedContract request)
    {
        if ( verdict == "ce" )
        {
            return Truncate(
                FirstNotEmpty(request.Compile.Stderr , request.Compile.Stdout , request.Note) ,
                4000);
        }

        var firstFailed = request.Cases
            .OrderBy(x => x.Ordinal)
            .FirstOrDefault(x => NormalizeVerdict(x.Verdict) != "ac");

        if ( firstFailed is not null )
        {
            return Truncate(
                FirstNotEmpty(
                    firstFailed.Message ,
                    firstFailed.Stderr ,
                    firstFailed.CheckerMessage ,
                    firstFailed.Note ,
                    request.Note) ,
                4000);
        }

        return Truncate(request.Note , 4000);
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

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
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