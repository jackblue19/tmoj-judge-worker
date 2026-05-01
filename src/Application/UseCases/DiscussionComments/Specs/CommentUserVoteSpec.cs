using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs
{
    public class CommentUserVoteSpec : Specification<ContentVote>
    {
        public CommentUserVoteSpec(Guid userId, List<Guid> commentIds)
        {
            Query.Where(x => x.UserId == userId && commentIds.Contains(x.TargetId) && x.TargetType == "comment");
        }
    }
}