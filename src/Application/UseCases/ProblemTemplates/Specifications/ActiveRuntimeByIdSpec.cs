using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemTemplates.Specifications;

public sealed class ActiveRuntimeByIdSpec : Specification<Runtime>
{
    public ActiveRuntimeByIdSpec(Guid runtimeId)
    {
        Query
            .Where(x => x.Id == runtimeId && x.IsActive)
            .AsNoTracking();
    }
}