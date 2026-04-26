using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.BuyFptItem;

public record BuyFptItemCommand(Guid ItemId) : IRequest<Guid>;

public class BuyFptItemCommandHandler : IRequestHandler<BuyFptItemCommand, Guid>
{
    private readonly IFptItemRepository _itemRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IUserInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public BuyFptItemCommandHandler(
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
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException("Bạn cần đăng nhập để mua vật phẩm.");
        
        Guid currentUserId = userId.Value;

        // 1. Lấy vật phẩm
        var item = await _itemRepo.GetByIdAsync(request.ItemId);
        if (item == null || !item.IsActive) 
            throw new Exception("Vật phẩm không tồn tại hoặc đã ngừng bán.");

        // 2. Lấy ví tiền
        var wallet = await _walletRepo.GetByUserIdAsync(currentUserId);
        if (wallet == null) 
            throw new Exception("Bạn chưa có ví tiền. Vui lòng liên hệ Admin.");

        // 3. Kiểm tra số dư
        if (wallet.Balance < item.PriceCoin)
        {
            throw new Exception($"Số dư không đủ. Bạn cần thêm {item.PriceCoin - wallet.Balance} Coin nữa.");
        }

        // 4. Thực hiện giao dịch (Tất cả thay đổi được EF Core theo dõi và thực hiện trong 1 transaction khi SaveChanges)
        
        // A. Trừ tiền
        wallet.Balance -= item.PriceCoin;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepo.UpdateAsync(wallet);

        // B. Ghi lịch sử ví
        var walletTx = new WalletTransaction
        {
            TransactionId = Guid.NewGuid(),
            WalletId = wallet.WalletId,
            Type = "purchase",
            Direction = "out",
            Amount = item.PriceCoin,
            SourceType = "fpt_items",
            SourceId = item.ItemId,
            Status = "completed",
            Metadata = $"Mua vật phẩm: {item.Name}",
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(walletTx);

        // C. Thêm vào kho đồ
        var inventory = new UserInventory
        {
            InventoryId = Guid.NewGuid(),
            UserId = currentUserId,
            ItemId = item.ItemId,
            AcquiredAt = DateTime.UtcNow,
            IsEquipped = false,
            TransactionId = walletTx.TransactionId
        };

        if (item.DurationDays.HasValue && item.DurationDays > 0)
        {
            inventory.ExpiresAt = DateTime.UtcNow.AddDays(item.DurationDays.Value);
        }

        await _inventoryRepo.AddAsync(inventory);

        // D. Lưu tất cả thay đổi
        await _uow.SaveChangesAsync(ct);

        return inventory.InventoryId;
    }
}
