namespace Application.UseCases.Contests.Dtos;

public class ContestDetailDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public string VisibilityCode { get; set; } = string.Empty;
    public string? ContestType { get; set; }

    public bool AllowTeams { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<ContestProblemDto> Problems { get; set; } = new();
}