using MediatR;

namespace Application.UseCases.Classes.Commands.JoinContest;

public record JoinContestCommand(
    Guid ClassSemesterId,
    Guid ContestId,
    Guid UserId
) : IRequest;
