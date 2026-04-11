using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByProblem;

public sealed class SubmissionsByProblemPagedSpec
    : Specification<Submission , SubmissionListItemDto>
{
    public SubmissionsByProblemPagedSpec(
        Guid problemId ,
        Guid currentUserId ,
        bool isElevated ,
        int page ,
        int pageSize)
    {
        if ( page < 1 ) page = 1;
        if ( pageSize < 1 ) pageSize = 1;

        var skip = (page - 1) * pageSize;

        Query.Where(x =>
            x.ProblemId == problemId &&
            !x.IsDeleted);

        if ( !isElevated )
        {
            Query.Where(x => x.UserId == currentUserId);
        }

        Query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize);

        Query.Select(x => new SubmissionListItemDto(
            x.Id ,
            x.UserId ,
            x.ProblemId ,
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