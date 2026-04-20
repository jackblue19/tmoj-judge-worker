using MediatR;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Commands.ToggleFavoriteContest;

public class ToggleFavoriteContestCommand
    : IRequest<ToggleFavoriteContestResponseDto> 
{
    public Guid UserId { get; set; }
    public Guid ContestId { get; set; }
}