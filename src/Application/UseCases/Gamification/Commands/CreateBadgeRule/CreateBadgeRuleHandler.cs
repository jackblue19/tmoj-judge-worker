using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Gamification.Commands.CreateBadgeRule;

public class CreateBadgeRuleHandler : IRequestHandler<CreateBadgeRuleCommand, Guid>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateBadgeRuleHandler> _logger;

    private static readonly string[] RuleTypes = { "rank", "streak_days", "solved_count", "complete_contest" };
    private static readonly string[] Targets = { "contest", "course", "org", "streak", "problem" };

    public CreateBadgeRuleHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser,
        ILogger<CreateBadgeRuleHandler> logger)
    {
        _repo = repo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateBadgeRuleCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("admin"))
            throw new UnauthorizedAccessException();

        if (!RuleTypes.Contains(request.RuleType))
            throw new ArgumentException("Invalid rule type");

        if (!Targets.Contains(request.TargetEntity))
            throw new ArgumentException("Invalid target");

        if (request.TargetValue <= 0)
            throw new ArgumentException("TargetValue must > 0");

        var rule = new BadgeRule
        {
            BadgeRulesId = Guid.NewGuid(),
            BadgeId = request.BadgeId,
            RuleType = request.RuleType,
            TargetEntity = request.TargetEntity,
            TargetValue = request.TargetValue,
            ScopeId = request.ScopeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.CreateBadgeRuleAsync(rule);
        await _repo.SaveChangesAsync();

        return rule.BadgeRulesId;
    }
}