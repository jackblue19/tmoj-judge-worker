using MediatR;

namespace Application.UseCases.DiscussionComments.Commands;

public class VoteCommentCommand : IRequest<bool>
{
    public Guid CommentId { get; }
    public int VoteType { get; }

    public VoteCommentCommand(Guid commentId, int voteType)
    {
        CommentId = commentId;
        VoteType = voteType;
    }
}