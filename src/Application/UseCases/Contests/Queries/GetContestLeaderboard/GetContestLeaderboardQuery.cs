using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestLeaderboardQuery : IRequest<GetContestLeaderboardResponse>
{
    public Guid ContestId { get; set; }
}
