using Application.Common.Interfaces;
using Application.UseCases.ProblemSolved.Dtos;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class ProblemSolvedQueryService : IProblemSolvedQueryService
{
    private readonly TmojDbContext _db;

    public ProblemSolvedQueryService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<ProblemSolvedStatsDto> GetSolvedStatsAsync(
        Guid userId ,
        string? visibilityCode ,
        string? solvedSourceCode ,
        CancellationToken cancellationToken = default)
    {
        visibilityCode = NormalizeNullableCode(visibilityCode);
        solvedSourceCode = NormalizeNullableCode(solvedSourceCode);

        ValidateSolvedSourceCode(solvedSourceCode);

        var query =
            from s in _db.Submissions.AsNoTracking()
            join p in _db.Problems.AsNoTracking()
                on s.ProblemId equals p.Id
            where s.UserId == userId
                  && s.IsDeleted == false
                  && s.StatusCode == "done"
                  && s.VerdictCode == "ac"
                  && p.IsActive == true
            select new SolvedProblemRow
            {
                ProblemId = p.Id ,
                VisibilityCode = p.VisibilityCode ,
                ContestProblemId = s.ContestProblemId ,
                ClassSlotId = s.ClassSlotId
            };

        if ( !string.IsNullOrWhiteSpace(visibilityCode) )
        {
            query = query.Where(x =>
                x.VisibilityCode != null &&
                x.VisibilityCode.ToLower() == visibilityCode);
        }

        if ( !string.IsNullOrWhiteSpace(solvedSourceCode) )
        {
            query = solvedSourceCode switch
            {
                "contest" => query.Where(x => x.ContestProblemId != null),

                "in-class" => query.Where(x => x.ClassSlotId != null),

                "practice" => query.Where(x =>
                    x.ContestProblemId == null &&
                    x.ClassSlotId == null),

                _ => query
            };
        }

        var totalSolved = await query
            .Select(x => x.ProblemId)
            .Distinct()
            .CountAsync(cancellationToken);

        var byVisibilityRows = await query
            .Select(x => new
            {
                x.ProblemId ,
                VisibilityCode = x.VisibilityCode ?? "unknown"
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var byVisibility = byVisibilityRows
            .GroupBy(x => x.VisibilityCode)
            .Select(g => new ProblemSolvedGroupDto
            {
                Code = g.Key ,
                Count = g.Select(x => x.ProblemId).Distinct().Count()
            })
            .OrderBy(x => x.Code)
            .ToList();

        var sourceBaseQuery =
            from s in _db.Submissions.AsNoTracking()
            join p in _db.Problems.AsNoTracking()
                on s.ProblemId equals p.Id
            where s.UserId == userId
                  && s.IsDeleted == false
                  && s.StatusCode == "done"
                  && s.VerdictCode == "ac"
                  && p.IsActive == true
            select new SolvedProblemRow
            {
                ProblemId = p.Id ,
                VisibilityCode = p.VisibilityCode ,
                ContestProblemId = s.ContestProblemId ,
                ClassSlotId = s.ClassSlotId
            };

        if ( !string.IsNullOrWhiteSpace(visibilityCode) )
        {
            sourceBaseQuery = sourceBaseQuery.Where(x =>
                x.VisibilityCode != null &&
                x.VisibilityCode.ToLower() == visibilityCode);
        }

        var sourceRows = await sourceBaseQuery
            .Select(x => new
            {
                x.ProblemId ,
                SourceCode =
                    x.ClassSlotId != null
                        ? "in-class"
                        : x.ContestProblemId != null
                            ? "contest"
                            : "practice"
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        if ( !string.IsNullOrWhiteSpace(solvedSourceCode) )
        {
            sourceRows = sourceRows
                .Where(x => x.SourceCode == solvedSourceCode)
                .ToList();
        }

        var bySource = sourceRows
            .GroupBy(x => x.SourceCode)
            .Select(g => new ProblemSolvedGroupDto
            {
                Code = g.Key ,
                Count = g.Select(x => x.ProblemId).Distinct().Count()
            })
            .OrderBy(x => x.Code)
            .ToList();

        return new ProblemSolvedStatsDto
        {
            UserId = userId ,
            VisibilityCode = visibilityCode ,
            SolvedSourceCode = solvedSourceCode ,
            TotalSolved = totalSolved ,
            ByVisibility = byVisibility ,
            BySource = bySource
        };
    }

    public async Task<ProblemSolvedListDto> GetSolvedProblemsAsync(
        Guid userId ,
        string? visibilityCode ,
        string? solvedSourceCode ,
        int page ,
        int pageSize ,
        CancellationToken cancellationToken = default)
    {
        visibilityCode = NormalizeNullableCode(visibilityCode);
        solvedSourceCode = NormalizeNullableCode(solvedSourceCode);

        ValidateSolvedSourceCode(solvedSourceCode);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var acQuery =
            from s in _db.Submissions.AsNoTracking()
            join p in _db.Problems.AsNoTracking()
                on s.ProblemId equals p.Id
            where s.UserId == userId
                  && s.IsDeleted == false
                  && s.StatusCode == "done"
                  && s.VerdictCode == "ac"
                  && p.IsActive == true
            select new SolvedSubmissionRow
            {
                ProblemId = p.Id ,
                Slug = p.Slug ,
                Title = p.Title ,
                Difficulty = p.Difficulty ,
                TypeCode = p.TypeCode ,
                VisibilityCode = p.VisibilityCode ,
                StatusCode = p.StatusCode ,
                ProblemMode = p.ProblemMode ,
                ProblemSource = p.ProblemSource ,

                SubmissionId = s.Id ,
                CreatedAt = s.CreatedAt ,
                TimeMs = s.TimeMs ,
                MemoryKb = s.MemoryKb ,

                ContestProblemId = s.ContestProblemId ,
                ClassSlotId = s.ClassSlotId
            };

        if ( !string.IsNullOrWhiteSpace(visibilityCode) )
        {
            acQuery = acQuery.Where(x =>
                x.VisibilityCode != null &&
                x.VisibilityCode.ToLower() == visibilityCode);
        }

        if ( !string.IsNullOrWhiteSpace(solvedSourceCode) )
        {
            acQuery = solvedSourceCode switch
            {
                "contest" => acQuery.Where(x => x.ContestProblemId != null),

                "in-class" => acQuery.Where(x => x.ClassSlotId != null),

                "practice" => acQuery.Where(x =>
                    x.ContestProblemId == null &&
                    x.ClassSlotId == null),

                _ => acQuery
            };
        }

        var rows = await acQuery.ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(x => x.ProblemId)
            .Select(g =>
            {
                var first = g
                    .OrderBy(x => x.CreatedAt)
                    .First();

                var last = g
                    .OrderByDescending(x => x.CreatedAt)
                    .First();

                var best = g
                    .OrderBy(x => x.TimeMs ?? int.MaxValue)
                    .ThenBy(x => x.MemoryKb ?? int.MaxValue)
                    .ThenBy(x => x.CreatedAt)
                    .First();

                var sourceCodes = g
                    .Select(GetSolvedSourceCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                return new ProblemSolvedItemDto
                {
                    ProblemId = first.ProblemId ,
                    Slug = first.Slug ,
                    Title = first.Title ,

                    Difficulty = first.Difficulty ,
                    TypeCode = first.TypeCode ,
                    VisibilityCode = first.VisibilityCode ,
                    StatusCode = first.StatusCode ,

                    ProblemMode = first.ProblemMode ,
                    ProblemSource = first.ProblemSource ,

                    AcceptedSubmissionsCount = g.Count() ,

                    FirstSolvedAt = first.CreatedAt ,
                    LastSolvedAt = last.CreatedAt ,

                    SolvedSourceCodes = sourceCodes ,

                    BestSubmissionId = best.SubmissionId ,
                    BestTimeMs = best.TimeMs ,
                    BestMemoryKb = best.MemoryKb
                };
            })
            .OrderByDescending(x => x.LastSolvedAt)
            .ThenBy(x => x.Title)
            .ToList();

        var totalCount = grouped.Count;

        var items = grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ProblemSolvedListDto
        {
            UserId = userId ,
            VisibilityCode = visibilityCode ,
            SolvedSourceCode = solvedSourceCode ,
            Page = page ,
            PageSize = pageSize ,
            TotalCount = totalCount ,
            Items = items
        };
    }

    private static string GetSolvedSourceCode(SolvedSubmissionRow row)
    {
        if ( row.ClassSlotId != null )
            return "in-class";

        if ( row.ContestProblemId != null )
            return "contest";

        return "practice";
    }

    private sealed class SolvedSubmissionRow
    {
        public Guid ProblemId { get; init; }

        public string? Slug { get; init; }
        public string Title { get; init; } = string.Empty;

        public string? Difficulty { get; init; }
        public string? TypeCode { get; init; }
        public string? VisibilityCode { get; init; }
        public string? StatusCode { get; init; }

        public string? ProblemMode { get; init; }
        public string? ProblemSource { get; init; }

        public Guid SubmissionId { get; init; }
        public DateTime CreatedAt { get; init; }
        public int? TimeMs { get; init; }
        public int? MemoryKb { get; init; }

        public Guid? ContestProblemId { get; init; }
        public Guid? ClassSlotId { get; init; }
    }

    private static string? NormalizeNullableCode(string? value)
    {
        if ( string.IsNullOrWhiteSpace(value) )
            return null;

        return value.Trim().ToLowerInvariant();
    }

    private static void ValidateSolvedSourceCode(string? solvedSourceCode)
    {
        if ( string.IsNullOrWhiteSpace(solvedSourceCode) )
            return;

        var valid = solvedSourceCode is "practice"
            or "contest"
            or "in-class";

        if ( !valid )
        {
            throw new InvalidOperationException(
                "Invalid solvedSourceCode. Supported values: practice, contest, in-class.");
        }
    }

    private sealed class SolvedProblemRow
    {
        public Guid ProblemId { get; init; }
        public string? VisibilityCode { get; init; }
        public Guid? ContestProblemId { get; init; }
        public Guid? ClassSlotId { get; init; }
    }
}