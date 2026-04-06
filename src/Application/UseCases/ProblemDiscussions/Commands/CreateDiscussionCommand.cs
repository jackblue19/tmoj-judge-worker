using MediatR;

namespace Application.UseCases.ProblemDiscussions.Commands;

public record CreateDiscussionCommand(
    Guid ProblemId,
    string Title,
    string Content
) : IRequest<Guid>;
