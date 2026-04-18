using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetBadgeRules;

public class GetBadgeRulesHandler : IRequestHandler<GetBadgeRulesQuery, List<BadgeRuleDto>>
{
    private readonly IGamificationRepository _repo;

    public GetBadgeRulesHandler(IGamificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<BadgeRuleDto>> Handle(GetBadgeRulesQuery request, CancellationToken ct)
    {
        var rules = await _repo.GetAllBadgeRulesAsync();

        return rules.Select(x => new BadgeRuleDto
        {
            Id = x.BadgeRulesId,
            BadgeId = x.BadgeId,
            RuleType = x.RuleType,
            TargetEntity = x.TargetEntity,
            TargetValue = x.TargetValue,
            IsActive = x.IsActive
        }).ToList();
    }
}