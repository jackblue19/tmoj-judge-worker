using MediatR;

namespace Application.UseCases.DiscussionComments.Commands;

public record UpdateDiscussionCommentCommand(
    Guid CommentId,
    string Content
) : IRequest<bool>;