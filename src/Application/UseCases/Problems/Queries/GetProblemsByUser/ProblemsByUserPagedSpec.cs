using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetProblemsByUser;

public sealed class ProblemsByUserPagedSpec
    : Specification<UserProblemStat , ProblemByUserListItemDto>
{
    public ProblemsByUserPagedSpec(
        Guid userId ,
        int page ,
        int pageSize)
    {
        if ( page < 1 ) page = 1;
        if ( pageSize < 1 ) pageSize = 1;

        var skip = (page - 1) * pageSize;

        Query.Where(x => x.UserId == userId);

        Query
            .OrderByDescending(x => x.LastSubmissionAt)
            .ThenByDescending(x => x.ProblemId)
            .Skip(skip)
            .Take(pageSize);

        Query.Select(x => new ProblemByUserListItemDto(
            x.ProblemId ,
            x.Problem.Slug ?? string.Empty ,
            x.Problem.Title ,
            x.Problem.Difficulty ,
            x.Problem.TypeCode ,
            x.Problem.IsActive ,
            x.Attempts ,
            x.Solved ,
            x.BestSubmissionId ,
            x.LastSubmissionAt
        ));
    }
}