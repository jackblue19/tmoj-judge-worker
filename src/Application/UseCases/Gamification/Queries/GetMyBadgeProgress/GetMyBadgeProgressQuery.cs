using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyBadgeProgress;

public class GetMyBadgeProgressQuery : IRequest<List<BadgeProgressDto>>
{
    public Guid UserId { get; set; }
}