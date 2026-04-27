using Application.Common.Interfaces;
using Domain.Abstractions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.UpdateFptItem;

public class UpdateFptItemHandler : IRequestHandler<UpdateFptItemCommand, bool>
{
    private readonly IFptItemRepository _itemRepo;
    private readonly IUnitOfWork _uow;

    public UpdateFptItemHandler(IFptItemRepository itemRepo, IUnitOfWork uow)
    {
        _itemRepo = itemRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateFptItemCommand request, CancellationToken ct)
    {
        var item = await _itemRepo.GetByIdAsync(request.ItemId);
        if (item == null) return false;

        item.Name = request.Name;
        item.Description = request.Description;
        item.ItemType = request.ItemType;
        item.PriceCoin = request.PriceCoin;
        item.ImageUrl = request.ImageUrl;
        item.DurationDays = request.DurationDays;
        item.MetaJson = request.MetaJson;
        item.IsActive = request.IsActive;

        await _itemRepo.UpdateAsync(item);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
