using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

public sealed class SubmissionByIdSpec : Specification<Submission>
{
    public SubmissionByIdSpec(Guid submissionId)
    {
        Query.Where(s => s.Id == submissionId);
    }
}
