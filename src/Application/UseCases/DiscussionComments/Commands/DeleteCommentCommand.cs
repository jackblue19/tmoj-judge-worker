using MediatR;

namespace Application.UseCases.DiscussionComments.Commands;

public record DeleteCommentCommand(Guid CommentId) : IRequest<bool>;