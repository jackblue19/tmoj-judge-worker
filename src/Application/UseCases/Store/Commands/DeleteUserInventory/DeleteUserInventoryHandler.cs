using Application.Common.Interfaces;
using Domain.Abstractions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.DeleteUserInventory;

public class DeleteUserInventoryHandler : IRequestHandler<DeleteUserInventoryCommand, bool>
{
    private readonly IUserInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public DeleteUserInventoryHandler(
        IUserInventoryRepository inventoryRepo, 
        IUnitOfWork uow, 
        ICurrentUserService currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(DeleteUserInventoryCommand request, CancellationToken ct)
    {
        var inv = await _inventoryRepo.GetByIdAsync(request.InventoryId);
        if (inv == null) return false;

        if (inv.UserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa vật phẩm này.");

        await _inventoryRepo.DeleteAsync(inv);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
