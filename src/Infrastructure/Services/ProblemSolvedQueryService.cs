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