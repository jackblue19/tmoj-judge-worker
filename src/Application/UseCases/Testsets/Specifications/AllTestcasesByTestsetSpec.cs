using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Testsets.Specifications;

public sealed class AllTestcasesByTestsetSpec : Specification<Testcase>
{
    public AllTestcasesByTestsetSpec(Guid testsetId)
    {
        Query
            .Where(x => x.TestsetId == testsetId)
            .OrderBy(x => x.Ordinal);
    }
}