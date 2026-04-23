using MediatR;
using Domain.Entities;

namespace Application.UseCases.Gamification.Queries.GetAllBadges;

public class GetAllBadgesQuery : IRequest<List<Badge>>
{
}
