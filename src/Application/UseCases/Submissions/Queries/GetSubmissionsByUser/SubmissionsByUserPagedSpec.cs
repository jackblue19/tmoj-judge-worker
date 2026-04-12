using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByUser;

public sealed class SubmissionsByUserPagedSpec
    : Specification<Submission , SubmissionByUserListItemDto>
{
    public SubmissionsByUserPagedSpec(
        Guid userId ,
        int page ,
        int pageSize)
    {
        if ( page < 1 ) page = 1;
        if ( pageSize < 1 ) pageSize = 1;

        var skip = (page - 1) * pageSize;

        Query.Where(x =>
            x.UserId == userId &&
            !x.IsDeleted);

        Query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize);

        Query.Select(x => new SubmissionByUserListItemDto(
            x.Id ,
            x.UserId ,
            x.ProblemId ,
            x.RuntimeId ,
            x.StatusCode ,
            x.VerdictCode ,
            x.FinalScore ,
            x.TimeMs ,
            x.MemoryKb ,
            x.CreatedAt ,
            x.JudgedAt
        ));
    }
}