namespace Application.UseCases.Contests.Dtos;

public class ContestParticipantsResultDto
{
    public int TotalTeams { get; set; }
    public int TotalUsers { get; set; }
    public List<ContestParticipantTeamDto> Teams { get; set; } = new();
}

public class ContestParticipantTeamDto
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public string? TeamAvatarUrl { get; set; }
    public bool IsPersonal { get; set; }
    public Guid LeaderId { get; set; }
    public DateTime JoinAt { get; set; }
    public int? Rank { get; set; }
    public decimal? Score { get; set; }
    public int SolvedProblem { get; set; }
    public List<ContestParticipantUserDto> Members { get; set; } = new();
}

public class ContestParticipantUserDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Username { get; set; }
    public string? RollNumber { get; set; }
}
