using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemDiscussions.Specs;

public class DiscussionVoteSpec : Specification<ContentVote>
{
    public DiscussionVoteSpec(Guid userId, Guid discussionId)
    {
        Query.Where(v => v.UserId == userId && v.TargetId == discussionId && v.TargetType == "discussion");
    }
}