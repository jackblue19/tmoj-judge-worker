using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.Checkout;

public class CheckoutHandler : IRequestHandler<CheckoutCommand, bool>
{
    private readonly ICartItemRepository _cartRepo;
    private readonly IFptItemRepository _itemRepo;
    private readonly IUserInventoryRepository _inventoryRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CheckoutHandler(
        ICartItemRepository cartRepo, 
        IFptItemRepository itemRepo,
        IUserInventoryRepository inventoryRepo,
        IWalletRepository walletRepo,
        IUnitOfWork uow, 
        ICurrentUserService currentUser)
    {
        _cartRepo = cartRepo;
        _itemRepo = itemRepo;
        _inventoryRepo = inventoryRepo;
        _walletRepo = walletRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(CheckoutCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId == Guid.Empty)
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thanh toán.");

        // 1. Lấy tất cả item trong giỏ
        var cartItems = await _cartRepo.GetByUserIdAsync(userId.Value);
        if (!cartItems.Any())
            throw new Exception("Giỏ hàng của bạn đang trống.");

        // 2. Tính tổng tiền
        decimal totalCost = cartItems.Sum(x => x.Item.PriceCoin * x.Quantity);

        // 3. Kiểm tra ví
        var wallet = await _walletRepo.GetByUserIdAsync(userId.Value);
        if (wallet == null || wallet.Balance < totalCost)
            throw new Exception($"Số dư không đủ để thanh toán. Tổng tiền: {totalCost} Coin.");

        // 4. Trừ tiền và lưu giao dịch
        wallet.Balance -= totalCost;
        await _walletRepo.UpdateAsync(wallet);

        var transactionId = Guid.NewGuid();
        await _walletRepo.AddTransactionAsync(new WalletTransaction
        {
            TransactionId = transactionId,
            WalletId = wallet.WalletId,
            Amount = totalCost,
            Type = "withdraw",
            Direction = "out",
            SourceType = "store",
            Status = "completed",
            CreatedAt = DateTime.UtcNow
        });

        // 5. Thêm đồ vào kho (Inventory) và trừ tồn kho
        foreach (var cartItem in cartItems)
        {
            var item = cartItem.Item;
            
            // Kiểm tra tồn kho
            if (item.StockQuantity < cartItem.Quantity)
            {
                throw new Exception($"Sản phẩm '{item.Name}' không đủ số lượng trong kho. Hiện còn: {item.StockQuantity}");
            }

            // Trừ tồn kho
            item.StockQuantity -= cartItem.Quantity;
            await _itemRepo.UpdateAsync(item);
            
            // Kiểm tra xem đã có món này chưa để cộng dồn số lượng (nếu là đồ có thể cộng dồn)
            var existingInventory = await _inventoryRepo.GetByUserAndItemAsync(userId.Value, cartItem.ItemId);
            
            if (existingInventory != null)
            {
                existingInventory.Quantity += cartItem.Quantity;
                await _inventoryRepo.UpdateAsync(existingInventory);
            }
            else
            {
                var newInventory = new UserInventory
                {
                    InventoryId = Guid.NewGuid(),
                    UserId = userId.Value,
                    ItemId = cartItem.ItemId,
                    AcquiredAt = DateTime.UtcNow,
                    Quantity = cartItem.Quantity,
                    IsEquipped = false,
                    TransactionId = transactionId
                };

                // Tính toán ngày hết hạn nếu có
                if (item.DurationDays.HasValue)
                {
                    newInventory.ExpiresAt = DateTime.UtcNow.AddDays(item.DurationDays.Value);
                }

                await _inventoryRepo.AddAsync(newInventory);
            }
        }

        // 6. Xóa giỏ hàng
        _cartRepo.RemoveRange(cartItems);

        // 7. Lưu tất cả thay đổi
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
