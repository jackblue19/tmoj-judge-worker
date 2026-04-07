using MediatR;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class VoteDiscussionCommand : IRequest<bool>
{
    public Guid DiscussionId { get; }
    public int VoteType { get; }

    public VoteDiscussionCommand(Guid discussionId, int voteType)
    {
        DiscussionId = discussionId;
        VoteType = voteType;
    }
}