using MediatR;
using Application.UseCases.Gamification.Dtos;

namespace Application.UseCases.Gamification.Queries.GetBadgeRules;

public class GetBadgeRulesQuery : IRequest<List<BadgeRuleDto>>
{
}