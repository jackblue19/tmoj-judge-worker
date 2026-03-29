using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Testsets.Specifications;

public sealed class TestcasePreviewByTestsetSpec : Specification<Testcase>
{
    public TestcasePreviewByTestsetSpec(Guid testsetId , int take = 3)
    {
        Query
            .Where(x => x.TestsetId == testsetId)
            .OrderBy(x => x.Ordinal)
            .Take(take);
    }
}