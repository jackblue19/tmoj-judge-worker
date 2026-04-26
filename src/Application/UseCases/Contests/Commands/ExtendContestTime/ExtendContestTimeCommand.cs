using MediatR;

namespace Application.UseCases.Contests.Commands;

public class ExtendContestTimeCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
    public DateTime NewEndAt { get; set; }
}
