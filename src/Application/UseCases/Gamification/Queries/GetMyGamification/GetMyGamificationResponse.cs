namespace Application.UseCases.Gamification.Queries.GetMyGamification;

public class GetMyGamificationResponse
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int SolvedProblems { get; set; }

    public List<BadgeDto> Badges { get; set; } = new();
}

public class BadgeDto
{
    public Guid BadgeId { get; set; }
    public string Name { get; set; } = default!;
    public string? IconUrl { get; set; }
    public int Level { get; set; }
}