using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByUser;

public sealed class SubmissionsByUserCountSpec : Specification<Submission>
{
    public SubmissionsByUserCountSpec(Guid userId)
    {
        Query.Where(x =>
            x.UserId == userId &&
            !x.IsDeleted);
    }
}