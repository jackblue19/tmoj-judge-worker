using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs
{
    public class CommentUserVoteSpec : Specification<CommentVote>
    {
        public CommentUserVoteSpec(Guid userId, List<Guid> commentIds)
        {
            Query
                .Where(v => v.UserId == userId && commentIds.Contains(v.CommentId));
        }
    }
}