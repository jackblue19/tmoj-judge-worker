using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Queries.GetUserPublicCollections;

public class GetUserPublicCollectionsQuery : IRequest<PublicCollectionsResult>
{
    public Guid UserId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}