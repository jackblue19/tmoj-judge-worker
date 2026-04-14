using MediatR;

namespace Application.UseCases.Teams.Commands;

public record DeleteTeamCommand(Guid TeamId) : IRequest<bool>;