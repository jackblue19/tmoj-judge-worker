using MediatR;

namespace Application.UseCases.Classes.Commands.FreezeClassContest;

public record FreezeClassContestCommand(
    Guid ClassSemesterId,
    Guid ContestId,
    Guid UserId
) : IRequest;
