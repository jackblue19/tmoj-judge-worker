using MediatR;

namespace Application.UseCases.Contests.Commands;

public class JoinContestCommand : IRequest<Guid>
{
    public Guid ContestId { get; set; }
    public Guid TeamId { get; set; }
}