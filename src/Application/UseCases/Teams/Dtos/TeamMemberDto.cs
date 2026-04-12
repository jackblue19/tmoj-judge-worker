namespace Application.UseCases.Teams.Dtos;

public class TeamMemberDto
{
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
}