using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Application.UseCases.Teams.Commands;

public class CreateTeamCommand : IRequest<CreateTeamResponse>
{
    [Required]
    [MinLength(3)]
    public string TeamName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
}