using MediatR;
using Application.UseCases.Contests.Dtos;

namespace Application.UseCases.Contests.Queries;

public class GetMyTeamInContestQuery : IRequest<MyTeamInContestDto?>
{
    public Guid ContestId { get; set; }
}