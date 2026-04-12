using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Teams.Dtos;

public class TeamDto
{
    public Guid Id { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public Guid LeaderId { get; set; }

    public int TeamSize { get; set; }

    public bool IsPersonal { get; set; }

    public string? InviteCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<TeamMemberDto> Members { get; set; } = new();
}