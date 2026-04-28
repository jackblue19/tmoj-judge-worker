using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.AddToCart;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, bool>
{
    private readonly ICartItemRepository _cartRepo;
    private readonly IFptItemRepository _itemRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AddToCartHandler(
        ICartItemRepository cartRepo, 
        IFptItemRepository itemRepo, 
        IUnitOfWork uow, 
        ICurrentUserService currentUser)
    {
        _cartRepo = cartRepo;
        _itemRepo = itemRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(AddToCartCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId == Guid.Empty)
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thêm vào giỏ hàng.");

        var item = await _itemRepo.GetByIdAsync(request.ItemId);
        if (item == null || !item.IsActive)
            throw new Exception("Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");

        // Kiểm tra xem đã có trong giỏ hàng chưa
        var existingCartItem = await _cartRepo.GetByUserAndItemAsync(userId.Value, request.ItemId);

        if (existingCartItem != null)
        {
            // Nếu đã có thì cộng dồn số lượng
            existingCartItem.Quantity += request.Quantity;
            _cartRepo.Update(existingCartItem);
        }
        else
        {
            // Nếu chưa có thì tạo mới
            var newCartItem = new CartItem
            {
                CartItemId = Guid.NewGuid(),
                UserId = userId.Value,
                ItemId = request.ItemId,
                Quantity = request.Quantity,
                AddedAt = DateTime.UtcNow
            };
            await _cartRepo.AddAsync(newCartItem);
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
