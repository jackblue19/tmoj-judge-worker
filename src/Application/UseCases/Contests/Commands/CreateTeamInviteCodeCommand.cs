using MediatR;

namespace Application.UseCases.Contests.Commands;

public class CreateTeamInviteCodeCommand : IRequest<string>
{
    public Guid ContestId { get; set; }
}
