using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemDiscussions.Specs;

public class DiscussionVoteSpec : Specification<CommentVote>
{
    public DiscussionVoteSpec(Guid userId, Guid discussionId)
    {
        Query.Where(x =>
            x.UserId == userId &&
            x.CommentId == discussionId
        );
    }
}