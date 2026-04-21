using MediatR;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Queries.CheckFavorite;

public class CheckFavoriteQuery : IRequest<CheckFavoriteResponseDto>
{
    public Guid UserId { get; set; }

    public Guid? ProblemId { get; set; }
    public Guid? ContestId { get; set; }
}