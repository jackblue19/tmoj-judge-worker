using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestScoreboardQuery : IRequest<List<ContestScoreboardDto>>
{
    public Guid ContestId { get; set; }
}