using MediatR;

namespace Application.UseCases.Contests.Commands;

public class CreateVirtualContestCommand : IRequest<Guid>
{
    public Guid SourceContestId { get; set; }
}
