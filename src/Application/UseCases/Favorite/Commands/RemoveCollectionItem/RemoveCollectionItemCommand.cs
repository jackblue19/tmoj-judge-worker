using MediatR;

namespace Application.UseCases.Favorite.Commands.RemoveCollectionItem;

public class RemoveCollectionItemCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid CollectionId { get; set; }
    public Guid ItemId { get; set; }
}