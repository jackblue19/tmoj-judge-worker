using MediatR;

namespace Application.UseCases.Contests.Commands;

public class DeleteContestCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
}
