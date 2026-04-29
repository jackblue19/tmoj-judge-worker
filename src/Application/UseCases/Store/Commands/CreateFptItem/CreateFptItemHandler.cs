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
        if (itemType.Contains("khung") || itemType.Contains("frame")) itemType = "avatar_frame";
        else if (itemType.Contains("nen") || itemType.Contains("bg") || itemType.Contains("background") || itemType.Contains("theme")) itemType = "profile_theme";
        else if (itemType.Contains("color") || itemType.Contains("mau")) itemType = "name_color";
        else if (itemType.Contains("danhhieu") || itemType.Contains("title") || itemType.Contains("bietdanh") || itemType.Contains("nickname")) itemType = "title";
        else if (itemType.Contains("huyhieu") || itemType.Contains("badge")) itemType = "badge";
        else if (itemType.Contains("consumable") || itemType.Contains("tieudung")) itemType = "consumable";
        else if (itemType.Contains("vatly") || itemType.Contains("physical")) itemType = "physical_item";

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
