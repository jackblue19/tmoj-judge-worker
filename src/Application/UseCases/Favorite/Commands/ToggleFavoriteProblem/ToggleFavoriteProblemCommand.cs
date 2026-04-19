using MediatR;

namespace Application.UseCases.Favorite.Commands.ToggleFavoriteProblem;

public class ToggleFavoriteProblemCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid ProblemId { get; set; }
}