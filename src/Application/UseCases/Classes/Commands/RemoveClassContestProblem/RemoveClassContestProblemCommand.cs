using MediatR;

namespace Application.UseCases.Classes.Commands.RemoveClassContestProblem;

public record RemoveClassContestProblemCommand(
    Guid ClassSemesterId,
    Guid ContestId,
    Guid ContestProblemId
) : IRequest;
