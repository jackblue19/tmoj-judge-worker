using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs
{
    public class CommentVoteAggregateSpec : Specification<CommentVote>
    {
        public CommentVoteAggregateSpec(List<Guid> commentIds)
        {
            Query
                .Where(v => commentIds.Contains(v.CommentId));
        }
    }
}