using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Gamification.Commands.UpdateBadgeRule;

public class UpdateBadgeRuleHandler : IRequestHandler<UpdateBadgeRuleCommand, bool>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

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