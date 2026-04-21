using MediatR;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Queries.GetFavoriteContests;

public class GetFavoriteContestsQuery
    : IRequest<PagedResult<FavoriteContestDto>>
{
    public Guid UserId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}