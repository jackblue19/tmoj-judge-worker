using MediatR;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Queries.GetFavoriteProblems;

public class GetFavoriteProblemsQuery
    : IRequest<PagedResult<FavoriteProblemDto>>
{
    public Guid UserId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}