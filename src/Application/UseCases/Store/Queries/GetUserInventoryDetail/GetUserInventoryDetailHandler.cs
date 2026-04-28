using Application.Common.Interfaces;
using Application.UseCases.Store.Dtos;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Queries.GetUserInventoryDetail;

public class GetUserInventoryDetailHandler : IRequestHandler<GetUserInventoryDetailQuery, UserInventoryDto?>
{
    private readonly IUserInventoryRepository _inventoryRepo;
    private readonly ICurrentUserService _currentUser;

    public GetUserInventoryDetailHandler(IUserInventoryRepository inventoryRepo, ICurrentUserService currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _currentUser = currentUser;
    }

    public async Task<UserInventoryDto?> Handle(GetUserInventoryDetailQuery request, CancellationToken ct)
    {
        var inv = await _inventoryRepo.GetByIdAsync(request.InventoryId);
        if (inv == null) return null;

        if (inv.UserId != _currentUser.UserId)
            throw new UnauthorizedAccessException();

        return new UserInventoryDto
        {
            InventoryId = inv.InventoryId,
            ItemId = inv.ItemId,
            ItemName = inv.Item.Name,
            ItemImageUrl = inv.Item.ImageUrl,
            ItemType = inv.Item.ItemType,
            AcquiredAt = inv.AcquiredAt,
            ExpiresAt = inv.ExpiresAt,
            Quantity = inv.Quantity,
            IsEquipped = inv.IsEquipped
        };
    }
}
