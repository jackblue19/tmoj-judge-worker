using Application.Common.Interfaces;
using Domain.Abstractions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.RemoveFromCart;

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, bool>
{
    private readonly ICartItemRepository _cartRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public RemoveFromCartHandler(
        ICartItemRepository cartRepo, 
        IUnitOfWork uow, 
        ICurrentUserService currentUser)
    {
        _cartRepo = cartRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(RemoveFromCartCommand request, CancellationToken ct)
    {
        var items = await _cartRepo.GetByUserIdAsync(_currentUser.UserId ?? Guid.Empty);
        var itemToRemove = items.FirstOrDefault(x => x.CartItemId == request.CartItemId);

        if (itemToRemove == null)
            throw new Exception("Vật phẩm không tồn tại trong giỏ hàng.");

        _cartRepo.Remove(itemToRemove);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
