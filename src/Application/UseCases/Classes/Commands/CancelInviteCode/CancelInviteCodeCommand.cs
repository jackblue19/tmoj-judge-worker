using MediatR;

namespace Application.UseCases.Classes.Commands.CancelInviteCode;

public record CancelInviteCodeCommand(Guid ClassSemesterId) : IRequest;
