using MediatR;

namespace Application.UseCases.Classes.Commands.JoinClassByCode;

public record JoinClassByCodeCommand(string Code, Guid UserId) : IRequest<(Guid ClassId, Guid ClassSemesterId)>;
