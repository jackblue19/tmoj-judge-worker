using MediatR;

namespace Application.UseCases.Contests.Commands;

public class ToggleEditorialVisibilityCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
    public Guid ContestProblemId { get; set; }
    public bool ShowEditorial { get; set; }
}
