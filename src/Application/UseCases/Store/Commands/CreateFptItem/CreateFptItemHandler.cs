using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.CreateFptItem;

public class CreateFptItemHandler : IRequestHandler<CreateFptItemCommand, Guid>
{
    private readonly IFptItemRepository _itemRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateFptItemHandler(
        IFptItemRepository itemRepo, 
        IUnitOfWork uow, 
        ICurrentUserService currentUser)
    {
        _itemRepo = itemRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateFptItemCommand request, CancellationToken ct)
    {
        var adminId = _currentUser.UserId;

        var newItem = new FptItem
        {
            Name = request.Name,
            Description = request.Description,
            ItemType = request.ItemType.Trim().ToLower(),
            PriceCoin = request.PriceCoin,
            ImageUrl = request.ImageUrl,
            DurationDays = request.DurationDays,
            StockQuantity = request.StockQuantity,
            MetaJson = request.MetaJson?.GetRawText(),
            IsActive = true,
            CreatedBy = adminId,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _itemRepo.AddAsync(newItem);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"DEBUG DB Error: {inner}");
        }

        return newItem.ItemId;
    }
}
