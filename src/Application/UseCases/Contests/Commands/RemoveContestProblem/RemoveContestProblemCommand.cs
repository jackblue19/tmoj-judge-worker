using MediatR;

namespace Application.UseCases.Contests.Commands;

public class RemoveContestProblemCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
    public Guid ContestProblemId { get; set; }
}
