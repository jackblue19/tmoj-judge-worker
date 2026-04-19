using MediatR;
using Application.UseCases.Favorite.Dtos; // 👈 dùng DTO chung

namespace Application.UseCases.Favorite.Commands.ReorderCollectionItems;

public class ReorderCollectionItemsCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid CollectionId { get; set; }

    // 👇 dùng DTO chuẩn
    public List<ReorderItemDto> Items { get; set; } = new();
}