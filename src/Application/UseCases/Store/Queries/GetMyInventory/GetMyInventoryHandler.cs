using Application.Common.Interfaces;
using Application.UseCases.Store.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Queries.GetMyInventory;

public class GetMyInventoryHandler : IRequestHandler<GetMyInventoryQuery, List<UserInventoryDto>>
{
    private readonly IUserInventoryRepository _inventoryRepo;
    private readonly ICurrentUserService _currentUser;

    public GetMyInventoryHandler(IUserInventoryRepository inventoryRepo, ICurrentUserService currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _currentUser = currentUser;
    }

    public async Task<List<UserInventoryDto>> Handle(GetMyInventoryQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException();

        var list = await _inventoryRepo.GetByUserIdAsync(userId.Value);

        return list.Select(x => new UserInventoryDto
        {
            InventoryId = x.InventoryId,
            ItemId = x.ItemId,
            ItemName = x.Item.Name,
            ItemImageUrl = x.Item.ImageUrl,
            ItemType = x.Item.ItemType,
            AcquiredAt = x.AcquiredAt,
            ExpiresAt = x.ExpiresAt,
            Quantity = x.Quantity,
            IsEquipped = x.IsEquipped
        }).ToList();
    }
}
