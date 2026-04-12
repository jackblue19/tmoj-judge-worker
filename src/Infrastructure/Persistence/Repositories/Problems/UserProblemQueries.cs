using Application.Common.Pagination;
using Application.UseCases.Problems.Queries.GetProblemsByUser;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories.Problems;

public sealed class UserProblemQueries : IUserProblemQueries
{
    private readonly TmojDbContext _db;

    public UserProblemQueries(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<ApiPagedResponse<ProblemByUserListItemDto>> GetProblemsByUserAsync(
        Guid userId ,
        int page ,
        int pageSize ,
        string? traceId ,
        CancellationToken ct = default)
    {
        if ( page < 1 ) page = 1;
        if ( pageSize < 1 ) pageSize = 1;

        var baseQuery = _db.Submissions
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted);

        var groupedQuery =
            from s in baseQuery
            group s by s.ProblemId into g
            select new
            {
                ProblemId = g.Key ,
                Attempts = g.Count() ,
                Solved = g.Any(x => x.VerdictCode == "ac") ,
                LastSubmissionAt = g.Max(x => (DateTime?) x.CreatedAt) ,
                BestSubmissionId = g
                    .OrderByDescending(x => x.VerdictCode == "ac")
                    .ThenByDescending(x => x.CreatedAt)
                    .Select(x => (Guid?) x.Id)
                    .FirstOrDefault()
            };

        var totalCount = await groupedQuery.LongCountAsync(ct);

        var items = await (
            from g in groupedQuery
            join p in _db.Problems.AsNoTracking()
                on g.ProblemId equals p.Id
            orderby g.LastSubmissionAt descending, g.ProblemId descending
            select new ProblemByUserListItemDto(
                g.ProblemId ,
                p.Slug ?? string.Empty ,
                p.Title ,
                p.Difficulty ,
                p.TypeCode ,
                p.IsActive ,
                g.Attempts ,
                g.Solved ,
                g.BestSubmissionId ,
                g.LastSubmissionAt
            )
        )
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

        var totalPages = totalCount == 0
            ? 0
            : (long) Math.Ceiling(totalCount / (double) pageSize);

        var pagination = new PaginationMeta(
            Page: page ,
            PageSize: pageSize ,
            TotalCount: totalCount ,
            TotalPages: totalPages ,
            HasPrevious: page > 1 ,
            HasNext: page < totalPages
        );

        return ApiPagedResponse<ProblemByUserListItemDto>.Ok(
            data: items ,
            pagination: pagination ,
            message: "Fetched problems by user successfully." ,
            traceId: traceId
        );
    }
}