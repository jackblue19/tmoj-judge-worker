using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.BuyFptItem;

public class BuyFptItemHandler : IRequestHandler<BuyFptItemCommand, Guid>
{
    private readonly IFptItemRepository _itemRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IUserInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public BuyFptItemHandler(
        IFptItemRepository itemRepo,
        IWalletRepository walletRepo,
        IUserInventoryRepository inventoryRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _itemRepo = itemRepo;
        _walletRepo = walletRepo;
        _inventoryRepo = inventoryRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(BuyFptItemCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException("Bạn cần đăng nhập để mua đồ.");

        var item = await _itemRepo.GetByIdAsync(request.ItemId)
            ?? throw new Exception("Vật phẩm không tồn tại.");

        var wallet = await _walletRepo.GetByUserIdAsync(userId.Value)
            ?? throw new Exception("Bạn chưa có ví tiền. Vui lòng liên hệ Admin.");

        if (wallet.Balance < item.PriceCoin)
        {
            throw new Exception($"Số dư không đủ. Bạn cần thêm {item.PriceCoin - wallet.Balance} Coin nữa.");
        }

        if (item.StockQuantity <= 0)
        {
            throw new Exception("Vật phẩm này đã hết hàng.");
        }

        wallet.Balance -= item.PriceCoin;
        wallet.UpdatedAt = DateTime.UtcNow;
        item.StockQuantity -= 1;
        
        await _walletRepo.UpdateAsync(wallet);
        await _itemRepo.UpdateAsync(item);

        var transactionId = Guid.NewGuid();
        await _walletRepo.AddTransactionAsync(new WalletTransaction
        {
            TransactionId = transactionId,
            WalletId = wallet.WalletId,
            Amount = item.PriceCoin,
            Type = "withdraw",
            Direction = "out",
            SourceType = "store",
            Status = "completed",
            CreatedAt = DateTime.UtcNow
        });

        // Kiểm tra xem đã có món này chưa để cộng dồn số lượng
        var existingInventory = await _inventoryRepo.GetByUserAndItemAsync(userId.Value, item.ItemId);

        if (existingInventory != null)
        {
            existingInventory.Quantity += 1;
            await _inventoryRepo.UpdateAsync(existingInventory);
            await _uow.SaveChangesAsync(ct);
            return existingInventory.InventoryId;
        }

        var inventory = new UserInventory
        {
            InventoryId = Guid.NewGuid(),
            UserId = userId.Value,
            ItemId = item.ItemId,
            AcquiredAt = DateTime.UtcNow,
            Quantity = 1,
            IsEquipped = false,
            TransactionId = transactionId
        };

        if (item.DurationDays.HasValue)
        {
            inventory.ExpiresAt = DateTime.UtcNow.AddDays(item.DurationDays.Value);
        }

        await _inventoryRepo.AddAsync(inventory);
        await _uow.SaveChangesAsync(ct);

        return inventory.InventoryId;
    }
}
