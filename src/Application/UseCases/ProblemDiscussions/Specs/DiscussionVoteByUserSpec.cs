using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemDiscussions.Specs;

public class DiscussionVoteByUserSpec : Specification<ContentReport>
{
    public DiscussionVoteByUserSpec(Guid userId, Guid discussionId)
    {
        Query.Where(x =>
            x.ReporterId == userId &&
            x.TargetId == discussionId &&
            x.TargetType == "discussion_vote");
    }
}