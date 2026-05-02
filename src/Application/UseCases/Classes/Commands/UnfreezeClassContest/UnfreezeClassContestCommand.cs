using MediatR;

namespace Application.UseCases.Classes.Commands.UnfreezeClassContest;

public record UnfreezeClassContestCommand(
    Guid ClassSemesterId,
    Guid ContestId,
    Guid UserId
) : IRequest;
