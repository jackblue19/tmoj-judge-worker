using MediatR;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Queries.GetCollectionDetail;

public class GetCollectionDetailQuery : IRequest<CollectionDetailDto>
{
    public Guid CollectionId { get; set; }
    public Guid UserId { get; set; }
}