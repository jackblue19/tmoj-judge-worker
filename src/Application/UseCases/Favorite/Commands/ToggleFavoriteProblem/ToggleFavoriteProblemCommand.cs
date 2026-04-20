using MediatR;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Commands.ToggleFavoriteProblem;

public class ToggleFavoriteProblemCommand : IRequest<ToggleFavoriteProblemResponseDto>
{
    public Guid UserId { get; set; }
    public Guid ProblemId { get; set; }
}