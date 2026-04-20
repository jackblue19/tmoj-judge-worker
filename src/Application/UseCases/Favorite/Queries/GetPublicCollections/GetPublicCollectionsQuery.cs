using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Queries.GetPublicCollections;

public class GetPublicCollectionsQuery : IRequest<PublicCollectionsResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}