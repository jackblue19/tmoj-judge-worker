using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Commands.GenerateInviteCode;

public record GenerateInviteCodeCommand(Guid ClassSemesterId, int MinutesValid) : IRequest<InviteCodeDto>;
