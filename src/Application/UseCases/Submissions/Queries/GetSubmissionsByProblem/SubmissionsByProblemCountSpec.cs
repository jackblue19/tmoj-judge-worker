using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByProblem;

public sealed class SubmissionsByProblemCountSpec : Specification<Submission>
{
    public SubmissionsByProblemCountSpec(
        Guid problemId ,
        Guid currentUserId ,
        bool isElevated)
    {
        Query.Where(x =>
            x.ProblemId == problemId &&
            !x.IsDeleted);

        if ( !isElevated )
        {
            Query.Where(x => x.UserId == currentUserId);
        }
    }
}