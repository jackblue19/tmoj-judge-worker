using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestParticipantsQuery : IRequest<List<ContestParticipantTeamDto>>
{
    public Guid ContestId { get; set; }
}
