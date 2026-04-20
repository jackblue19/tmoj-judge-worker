using MediatR;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Queries.GetMyCollections;

public class GetMyCollectionsQuery : IRequest<List<CollectionDto>>
{
    public Guid UserId { get; set; }
}