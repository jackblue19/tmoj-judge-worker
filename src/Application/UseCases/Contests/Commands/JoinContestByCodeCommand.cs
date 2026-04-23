using MediatR;

namespace Application.UseCases.Contests.Commands;

public class JoinContestByCodeCommand : IRequest<JoinByCodeResult>
{
    public string InviteCode { get; set; } = null!;
}
