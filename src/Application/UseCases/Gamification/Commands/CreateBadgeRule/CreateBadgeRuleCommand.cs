using MediatR;

namespace Application.UseCases.Gamification.Commands.CreateBadgeRule;

public class CreateBadgeRuleCommand : IRequest<Guid>
{
    public Guid BadgeId { get; set; }
    public string RuleType { get; set; } = default!; // rank | streak_days | solved_count
    public string TargetEntity { get; set; } = default!; // contest | course | org | streak | problem
    public int TargetValue { get; set; }
    public Guid? ScopeId { get; set; }
}