using Application.UseCases.Teams.Dtos;

namespace Application.UseCases.Contests.Dtos;

public class MyTeamInContestDto
{
    public Guid ContestId { get; set; }

    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;

    public Guid LeaderId { get; set; }

    public int TeamSize { get; set; }

    public int MemberCount { get; set; }

    public DateTime JoinedAt { get; set; }

    public int? Rank { get; set; }
    public decimal? Score { get; set; }

    public List<TeamMemberDto> Members { get; set; } = new();
}