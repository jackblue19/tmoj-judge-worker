namespace Application.UseCases.Gamification.Dtos;

public class UserBadgeDto
{
    public Guid UserBadgeId { get; set; }
    public Guid BadgeId { get; set; }

    public string Name { get; set; } = default!;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }

    public DateTime AwardedAt { get; set; }
}