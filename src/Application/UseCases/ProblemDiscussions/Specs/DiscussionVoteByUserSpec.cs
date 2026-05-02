using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemDiscussions.Specs;

public class DiscussionVoteByUserSpec : Specification<ContentVote>
{
    public DiscussionVoteByUserSpec(Guid userId, Guid discussionId)
    {
        Query.Where(v => v.UserId == userId && v.TargetId == discussionId && v.TargetType == "discussion");
    }
}