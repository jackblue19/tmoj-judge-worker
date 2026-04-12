using MediatR;

namespace Application.UseCases.Teams.Commands;

public class RemoveTeamMemberCommand : IRequest
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
}