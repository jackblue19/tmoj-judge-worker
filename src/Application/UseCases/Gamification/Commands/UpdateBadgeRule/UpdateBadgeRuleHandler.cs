using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Gamification.Commands.UpdateBadgeRule;

public class UpdateBadgeRuleHandler : IRequestHandler<UpdateBadgeRuleCommand, bool>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    private static readonly string[] RuleTypes = { "rank", "streak_days", "solved_count", "complete_contest" };
    private static readonly string[] Targets = { "contest", "course", "org", "streak", "problem" };

    public UpdateBadgeRuleHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(UpdateBadgeRuleCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("admin"))
            throw new UnauthorizedAccessException();

        if (!RuleTypes.Contains(request.RuleType))
            throw new ArgumentException("Invalid rule type");

        if (!Targets.Contains(request.TargetEntity))
            throw new ArgumentException("Invalid target");

        if (request.TargetValue <= 0)
            throw new ArgumentException("TargetValue must > 0");

        var rule = await _repo.GetBadgeRuleByIdAsync(request.Id);
        if (rule == null) return false;

        rule.RuleType = request.RuleType;
        rule.TargetEntity = request.TargetEntity;
        rule.TargetValue = request.TargetValue;
        rule.ScopeId = request.ScopeId;

        await _repo.UpdateBadgeRuleAsync(rule);
        await _repo.SaveChangesAsync();

        return true;
    }
}