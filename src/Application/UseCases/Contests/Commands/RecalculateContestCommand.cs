using MediatR;

namespace Application.UseCases.Contests.Commands;

public class RecalculateContestCommand : IRequest<Guid>
{
    public Guid ContestId { get; set; }
}
