using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;
using WebAPI.Models.Submissions;

namespace WebAPI.Services.Judging;

public sealed class SubmissionQueryService
{
    private readonly TmojDbContext _db;

    public SubmissionQueryService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<SubmissionDetailDto?> GetDetailAsync(
        Guid submissionId ,
        CancellationToken ct)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .Where(x => x.Id == submissionId)
            .Select(x => new SubmissionDetailDto
            {
                SubmissionId = x.Id ,
                UserId = x.UserId ,
                ProblemId = x.ProblemId ,
                RuntimeId = x.RuntimeId ,
                TestsetId = x.TestsetId ,
                StatusCode = x.StatusCode ,
                VerdictCode = x.VerdictCode ,
                FinalScore = x.FinalScore ,
                TimeMs = x.TimeMs ,
                MemoryKb = x.MemoryKb ,
                RuntimeName = x.Runtime != null ? x.Runtime.RuntimeName : null ,
                RuntimeVersion = x.Runtime != null ? x.Runtime.RuntimeVersion : null ,
                SourceCode = x.SourceCode ,
                Note = x.Note ,
                CreatedAt = x.CreatedAt ,
                JudgedAt = x.JudgedAt
            })
            .FirstOrDefaultAsync(ct);

        if ( submission is null )
            return null;

        submission.LatestRun = await _db.JudgeRuns
            .AsNoTracking()
            .Where(x => x.SubmissionId == submissionId)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new SubmissionRunDto
            {
                JudgeRunId = x.Id ,
                WorkerId = x.WorkerId ,
                Status = x.Status ,
                DockerImage = x.DockerImage ,
                Limits = x.Limits ,
                Note = x.Note ,
                CompileExitCode = x.CompileExitCode ,
                CompileTimeMs = x.CompileTimeMs ,
                TotalTimeMs = x.TotalTimeMs ,
                TotalMemoryKb = x.TotalMemoryKb ,
                StartedAt = x.StartedAt ,
                FinishedAt = x.FinishedAt
            })
            .FirstOrDefaultAsync(ct);

        submission.Results = await _db.Results
            .AsNoTracking()
            .Where(x => x.SubmissionId == submissionId)
            .OrderBy(x => x.Testcase != null ? x.Testcase.Ordinal : int.MaxValue)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new SubmissionCaseResultDto
            {
                ResultId = x.Id ,
                TestcaseId = x.TestcaseId ,
                Ordinal = x.Testcase != null ? x.Testcase.Ordinal : null ,
                StatusCode = x.StatusCode ,
                RuntimeMs = x.RuntimeMs ,
                MemoryKb = x.MemoryKb ,
                CheckerMessage = x.CheckerMessage ,
                ExitCode = x.ExitCode ,
                Message = x.Message ,
                Note = x.Note ,
                ExpectedOutput = x.ExpectedOutput ,
                ActualOutput = x.ActualOutput ,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return submission;
    }

    public async Task<PagedResponse<SubmissionListItemDto>> SearchAsync(
        SubmissionSearchRequest req ,
        CancellationToken ct)
    {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize <= 0 ? 20 : req.PageSize;
        if ( pageSize > 100 ) pageSize = 100;

        var query = _db.Submissions
            .AsNoTracking()
            .AsQueryable();

        if ( req.UserId.HasValue )
            query = query.Where(x => x.UserId == req.UserId.Value);

        if ( req.ProblemId.HasValue )
            query = query.Where(x => x.ProblemId == req.ProblemId.Value);

        if ( req.RuntimeId.HasValue )
            query = query.Where(x => x.RuntimeId == req.RuntimeId.Value);

        if ( !string.IsNullOrWhiteSpace(req.StatusCode) )
        {
            var normalizedStatus = req.StatusCode.Trim().ToLowerInvariant();
            query = query.Where(x => x.StatusCode == normalizedStatus);
        }

        if ( !string.IsNullOrWhiteSpace(req.VerdictCode) )
        {
            var normalizedVerdict = req.VerdictCode.Trim().ToLowerInvariant();
            query = query.Where(x => x.VerdictCode == normalizedVerdict);
        }

        if ( req.CreatedFromUtc.HasValue )
            query = query.Where(x => x.CreatedAt >= req.CreatedFromUtc.Value);

        if ( req.CreatedToUtc.HasValue )
            query = query.Where(x => x.CreatedAt <= req.CreatedToUtc.Value);

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SubmissionListItemDto
            {
                SubmissionId = x.Id ,
                UserId = x.UserId ,
                ProblemId = x.ProblemId ,
                RuntimeId = x.RuntimeId ,
                StatusCode = x.StatusCode ,
                VerdictCode = x.VerdictCode ,
                FinalScore = x.FinalScore ,
                TimeMs = x.TimeMs ,
                MemoryKb = x.MemoryKb ,
                RuntimeName = x.Runtime != null ? x.Runtime.RuntimeName : null ,
                RuntimeVersion = x.Runtime != null ? x.Runtime.RuntimeVersion : null ,
                CreatedAt = x.CreatedAt ,
                JudgedAt = x.JudgedAt
            })
            .ToListAsync(ct);

        return new PagedResponse<SubmissionListItemDto>
        {
            Page = page ,
            PageSize = pageSize ,
            TotalItems = totalItems ,
            TotalPages = (int) Math.Ceiling(totalItems / (double) pageSize) ,
            Items = items
        };
    }
}