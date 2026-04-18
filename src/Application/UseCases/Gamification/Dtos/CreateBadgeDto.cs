namespace Application.UseCases.Gamification.Dtos;

public class CreateBadgeDto
{
    public string Name { get; set; } = default!;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }

    public string BadgeCode { get; set; } = default!;
    public string BadgeCategory { get; set; } = default!; // contest | course | org | streak | problem

    public int BadgeLevel { get; set; } = 1;
    public bool IsRepeatable { get; set; } = false;
}