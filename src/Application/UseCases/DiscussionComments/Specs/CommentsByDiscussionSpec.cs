using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs
{
    public class CommentsByDiscussionSpec : Specification<DiscussionComment>
    {
        public CommentsByDiscussionSpec(Guid discussionId)
        {
            Query
                .Where(c => c.DiscussionId == discussionId)
                .OrderBy(c => c.CreatedAt);
        }
    }
}