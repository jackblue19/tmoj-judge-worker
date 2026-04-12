namespace Application.UseCases.Teams.Commands;

public class CreateTeamResponse
{
    public Guid TeamId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
}