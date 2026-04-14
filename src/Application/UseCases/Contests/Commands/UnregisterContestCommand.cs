using MediatR;

namespace Application.UseCases.Contests.Commands;

public class UnregisterContestCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
}