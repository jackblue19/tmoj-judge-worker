using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public sealed class ProblemByIdSpec : Specification<Problem>
{
    public ProblemByIdSpec(Guid problemId)
    {
        Query.Where(p => p.Id == problemId);
    }
}
