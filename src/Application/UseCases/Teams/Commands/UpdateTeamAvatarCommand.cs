using MediatR;

namespace Application.UseCases.Teams.Commands;

public class UpdateTeamAvatarCommand : IRequest
{
    public Guid TeamId { get; set; }
    public string? AvatarUrl { get; set; }
}
