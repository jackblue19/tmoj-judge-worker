using Application.Common.Interfaces;
using Domain.Abstractions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.UpdateUserInventory;

public class UpdateUserInventoryHandler : IRequestHandler<UpdateUserInventoryCommand, bool>
{
    private readonly IUserInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserInventoryHandler(
        IUserInventoryRepository inventoryRepo, 
        IUnitOfWork uow, 
        ICurrentUserService currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(UpdateUserInventoryCommand request, CancellationToken ct)
    {
        var inv = await _inventoryRepo.GetByIdAsync(request.InventoryId);
        if (inv == null) return false;

        // Bảo mật: Đảm bảo người dùng chỉ sửa được đồ của chính mình
        if (inv.UserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa vật phẩm này.");

        // Rule: Nếu item đã hết hạn thì không cho trang bị
        if (inv.ExpiresAt.HasValue && inv.ExpiresAt.Value < DateTime.UtcNow)
            throw new Exception("Vật phẩm đã hết hạn, không thể trang bị.");

        // Rule: Một loại chỉ được trang bị 1 cái (Mutual Exclusion)
        if (request.IsEquipped)
        {
            var currentlyEquipped = await _inventoryRepo.GetEquippedItemsByTypeAsync(inv.UserId, inv.Item.ItemType);
            foreach (var item in currentlyEquipped)
            {
                if (item.InventoryId != inv.InventoryId)
                {
                    item.IsEquipped = false;
                    await _inventoryRepo.UpdateAsync(item);
                }
            }
        }

        inv.IsEquipped = request.IsEquipped;

        await _inventoryRepo.UpdateAsync(inv);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
