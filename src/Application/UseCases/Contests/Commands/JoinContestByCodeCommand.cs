using MediatR;

namespace Application.UseCases.Contests.Commands;

public class JoinContestByCodeCommand : IRequest<Guid>
{
    public string InviteCode { get; set; } = null!;
}
