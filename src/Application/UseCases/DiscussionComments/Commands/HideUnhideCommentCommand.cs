using MediatR;
using System;

namespace Application.UseCases.DiscussionComments.Commands;

public class HideUnhideCommentCommand : IRequest<bool>
{
    public Guid CommentId { get; }
    public bool Hide { get; }

    public HideUnhideCommentCommand(Guid commentId, bool hide)
    {
        CommentId = commentId;
        Hide = hide;
    }
}