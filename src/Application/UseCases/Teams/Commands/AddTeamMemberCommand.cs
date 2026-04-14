using MediatR;

namespace Application.UseCases.Teams.Commands;

public class AddTeamMemberCommand : IRequest<Guid>
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
}