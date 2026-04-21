using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Queries.GetPublicCollections;

public class GetPublicCollectionsQuery : IRequest<PublicCollectionsResult>
{
    public Guid UserId { get; set; } // ✅ ADD
    public int Page { get; set; }
    public int PageSize { get; set; }
}