using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemTemplates.Specifications;

public sealed class ActiveProblemByIdSpec : Specification<Problem>
{
    public ActiveProblemByIdSpec(Guid problemId)
    {
        Query
            .Where(x => x.Id == problemId && x.IsActive)
            .AsNoTracking();
    }
}