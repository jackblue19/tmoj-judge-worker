namespace Application.UseCases.Gamification.Dtos;

public class BadgeRuleDto
{
    public Guid Id { get; set; }
    public Guid BadgeId { get; set; }
    public string RuleType { get; set; } = default!;
    public string TargetEntity { get; set; } = default!;
    public int TargetValue { get; set; }
    public bool IsActive { get; set; }
}