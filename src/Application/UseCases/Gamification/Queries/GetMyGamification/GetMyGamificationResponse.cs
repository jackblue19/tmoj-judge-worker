namespace Application.UseCases.Gamification.Queries.GetMyGamification;

public class GetMyGamificationResponse
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int SolvedProblems { get; set; }

    public int EasySolved { get; set; }
    public int EasyTotal { get; set; }
    public int MediumSolved { get; set; }
    public int MediumTotal { get; set; }
    public int HardSolved { get; set; }
    public int HardTotal { get; set; }

    public List<BadgeDto> Badges { get; set; } = new();
}

public class BadgeDto
{
    public Guid BadgeId { get; set; }
    public string Name { get; set; } = default!;
    public string? IconUrl { get; set; }
    public int Level { get; set; }
}