using MediatR;
using Application.UseCases.Gamification.Dtos;

namespace Application.UseCases.Gamification.Queries.GetMyBadges;

public record GetMyBadgesQuery()
    : IRequest<List<UserBadgeDto>>; 