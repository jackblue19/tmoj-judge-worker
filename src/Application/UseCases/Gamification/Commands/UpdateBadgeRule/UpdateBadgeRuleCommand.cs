using MediatR;

namespace Application.UseCases.Gamification.Commands.UpdateBadgeRule;

public class UpdateBadgeRuleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string RuleType { get; set; } = default!;
    public string TargetEntity { get; set; } = default!;
    public int TargetValue { get; set; }
    public Guid? ScopeId { get; set; }
}