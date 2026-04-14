namespace Application.UseCases.Teams.Dtos;

public class TeamMemberDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}