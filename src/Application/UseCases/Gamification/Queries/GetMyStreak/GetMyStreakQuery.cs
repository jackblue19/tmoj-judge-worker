using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyStreak;

public class GetMyStreakQuery : IRequest<StreakDto>
{
}