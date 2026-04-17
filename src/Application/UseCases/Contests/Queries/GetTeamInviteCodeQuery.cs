using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetTeamInviteCodeQuery : IRequest<string?>
{
    public Guid ContestId { get; set; }
}
