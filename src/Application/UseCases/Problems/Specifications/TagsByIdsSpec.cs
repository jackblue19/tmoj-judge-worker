using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class TagsByIdsSpec : Specification<Tag>
{
    public TagsByIdsSpec(IEnumerable<Guid> tagIds)
    {
        var ids = tagIds.Distinct().ToList();
        Query.Where(x => ids.Contains(x.Id) && x.IsActive);
    }
}