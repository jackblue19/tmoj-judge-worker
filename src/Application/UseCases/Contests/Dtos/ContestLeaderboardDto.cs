namespace Application.UseCases.Contests.Dtos;

public class ContestLeaderboardDto
{
    public Guid ContestId { get; set; }

    public List<TeamLeaderboardDto> Teams { get; set; } = new();
}
