using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetMyRewardHistory;

public class GetMyRewardHistoryQuery : IRequest<List<RewardHistoryDto>>
{
}