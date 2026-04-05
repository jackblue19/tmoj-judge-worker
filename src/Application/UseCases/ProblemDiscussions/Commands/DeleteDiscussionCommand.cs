using MediatR;

namespace Application.UseCases.ProblemDiscussions.Commands;

public record DeleteDiscussionCommand(Guid Id) : IRequest<bool>;