using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

public sealed class JudgeRunsBySubmissionSpec : Specification<JudgeRun>
{
    public JudgeRunsBySubmissionSpec(Guid submissionId)
    {
        Query.Where(jr => jr.SubmissionId == submissionId);
    }
}
