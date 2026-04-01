using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Testsets.Specifications;

public sealed class FirstThreeTestcasesByTestsetSpec : Specification<Testcase>
{
    public FirstThreeTestcasesByTestsetSpec(Guid testsetId)
    {
        Query
            .Where(x => x.TestsetId == testsetId && x.Ordinal >= 1 && x.Ordinal <= 3)
            .OrderBy(x => x.Ordinal);
    }
}