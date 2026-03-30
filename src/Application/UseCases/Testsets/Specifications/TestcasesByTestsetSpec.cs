using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Testsets.Specifications;

public sealed class TestcasesByTestsetSpec : Specification<Testcase>
{
    public TestcasesByTestsetSpec(Guid testsetId , int? take = null)
    {
        Query
            .Where(x => x.TestsetId == testsetId)
            .OrderBy(x => x.Ordinal);

        if ( take is > 0 )
            Query.Take(take.Value);
    }
}