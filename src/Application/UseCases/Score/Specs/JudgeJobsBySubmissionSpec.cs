using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

public sealed class JudgeJobsBySubmissionSpec : Specification<JudgeJob>
{
    public JudgeJobsBySubmissionSpec(Guid submissionId)
    {
        Query.Where(jj => jj.SubmissionId == submissionId);
    }
}
