using MediatR;

namespace Application.UseCases.Contests.Commands;

public class ArchiveContestCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
}
