using MediatR;

namespace Application.UseCases.Contests.Commands;

public class ChangeVisibilityCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
    public string VisibilityCode { get; set; } = null!;
}
