using MediatR;

namespace Application.UseCases.DiscussionComments.Commands;

public record CreateDiscussionCommentCommand(
    Guid DiscussionId,
    string Content,
    Guid? ParentId = null
) : IRequest<Guid>;