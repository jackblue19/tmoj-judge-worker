using MediatR;

namespace Application.UseCases.Contests.Commands;

public class UnfreezeContestCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
}