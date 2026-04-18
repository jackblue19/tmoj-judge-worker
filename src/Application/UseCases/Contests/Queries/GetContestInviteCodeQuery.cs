using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestInviteCodeQuery : IRequest<string?>
{
    public Guid ContestId { get; set; }
}
