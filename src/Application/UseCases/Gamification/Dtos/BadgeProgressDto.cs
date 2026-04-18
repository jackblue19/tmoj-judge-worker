namespace Application.UseCases.Gamification.Dtos;

public class BadgeProgressDto
{
    public Guid BadgeId { get; set; }
    public string Name { get; set; } = default!;
    public string? IconUrl { get; set; }

    public int CurrentValue { get; set; }
    public int TargetValue { get; set; }

    public double ProgressPercent { get; set; }
    public bool IsCompleted { get; set; }
}