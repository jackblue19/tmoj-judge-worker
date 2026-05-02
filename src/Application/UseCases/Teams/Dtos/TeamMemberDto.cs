namespace Application.UseCases.Teams.Dtos;

public class TeamMemberDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? EquippedFrameUrl { get; set; }
    public DateTime JoinedAt { get; set; }
}