namespace Application.UseCases.Contests.Dtos;

public class ContestDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public string VisibilityCode { get; set; } = string.Empty;
    public string? ContestType { get; set; }

    public bool AllowTeams { get; set; }

    public int TotalTeams { get; set; }
    public int TotalMembers { get; set; }

    public string Status { get; set; } = string.Empty;
}
