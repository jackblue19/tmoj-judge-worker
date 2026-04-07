using MediatR;

namespace Application.UseCases.Reports.Commands;

public record UnhideCommentCommand(Guid CommentId, bool IsHidden) : IRequest<Unit>;