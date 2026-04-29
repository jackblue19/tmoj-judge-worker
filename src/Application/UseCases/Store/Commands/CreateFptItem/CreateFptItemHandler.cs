using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using Application.Abstractions.Outbound.Services;
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
    private readonly ICloudinaryService _cloudinary;

    public CreateFptItemHandler(
        IFptItemRepository itemRepo, 
        IUnitOfWork uow, 
        ICurrentUserService currentUser,
        ICloudinaryService cloudinary)
    {
        _itemRepo = itemRepo;
        _uow = uow;
        _currentUser = currentUser;
        _cloudinary = cloudinary;
    }

    public async Task<Guid> Handle(CreateFptItemCommand request, CancellationToken ct)
    {
        var adminId = _currentUser.UserId;
        var imageUrl = request.ImageUrl;

        // 1. Xử lý Upload Ảnh nếu có Stream
        if (request.FileStream != null && !string.IsNullOrEmpty(request.Extension))
        {
            var imageId = await _cloudinary.UploadImageAsync(request.FileStream, request.Extension, "items", ct);
            imageUrl = _cloudinary.GetImageUrl(imageId, "items");
        }

        // 2. Chuẩn hóa ItemType (Avatar Frame, Title, Background, v.v.)
        // Một số giá trị phổ biến để tránh lỗi Check Constraint
        var itemType = request.ItemType.Trim().ToLower();
        if (itemType == "khung" || itemType == "frame") itemType = "avatar_frame";
        if (itemType == "nen" || itemType == "bg") itemType = "background";
        if (itemType == "danhhieu" || itemType == "title") itemType = "title";
        if (itemType == "huyhieu" || itemType == "badge") itemType = "badge";
        if (itemType == "vatly" || itemType == "physical") itemType = "physical";

        var newItem = new FptItem
        {
            Name = request.Name,
            Description = request.Description,
            ItemType = itemType,
            PriceCoin = request.PriceCoin,
            ImageUrl = imageUrl,
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
