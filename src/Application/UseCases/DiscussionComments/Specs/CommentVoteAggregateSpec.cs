using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs
{
    public class CommentVoteAggregateSpec : Specification<ContentVote>
    {
        public CommentVoteAggregateSpec(List<Guid> commentIds)
        {
            Query.Where(v => commentIds.Contains(v.TargetId) && v.TargetType == "comment");
        }
    }
}