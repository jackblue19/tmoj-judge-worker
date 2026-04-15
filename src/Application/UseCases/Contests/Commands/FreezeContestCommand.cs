using MediatR;

namespace Application.UseCases.Contests.Commands;

public class FreezeContestCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
}