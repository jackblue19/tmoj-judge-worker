using MediatR;
using System;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class HideUnhideDiscussionCommand : IRequest<bool>
{
    public Guid DiscussionId { get; }
    public bool Hide { get; }

    public HideUnhideDiscussionCommand(Guid discussionId, bool hide)
    {
        DiscussionId = discussionId;
        Hide = hide;
    }
}
