using Application.Common.Interfaces;
using Domain.Abstractions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.DeleteFptItem;

public class DeleteFptItemHandler : IRequestHandler<DeleteFptItemCommand, bool>
{
    private readonly IFptItemRepository _itemRepo;
    private readonly IUnitOfWork _uow;

    public DeleteFptItemHandler(IFptItemRepository itemRepo, IUnitOfWork uow)
    {
        _itemRepo = itemRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteFptItemCommand request, CancellationToken ct)
    {
        var item = await _itemRepo.GetByIdAsync(request.ItemId);
        if (item == null) return false;

        await _itemRepo.DeleteAsync(item);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
