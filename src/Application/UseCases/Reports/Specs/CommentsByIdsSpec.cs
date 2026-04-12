using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Reports.Specs;

public class CommentsByIdsSpec : Specification<DiscussionComment>
{
    public CommentsByIdsSpec(List<Guid> ids)
    {
        Query.Where(x => ids.Contains(x.Id));
    }
}