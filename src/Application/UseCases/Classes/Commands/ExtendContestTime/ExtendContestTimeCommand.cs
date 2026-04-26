using MediatR;

namespace Application.UseCases.Classes.Commands.ExtendContestTime;

public record ExtendContestTimeCommand(
    Guid ClassSemesterId,
    Guid ContestId,
    DateTime NewEndAt
) : IRequest;
