using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

public sealed class TestcasesByIdsSpec : Specification<Testcase>
{
    public TestcasesByIdsSpec(IReadOnlyCollection<Guid> ids)
    {
        Query.Where(t => ids.Contains(t.Id));
    }
}
