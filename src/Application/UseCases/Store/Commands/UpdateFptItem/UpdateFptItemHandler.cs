using Application.Common.Interfaces;
using Domain.Abstractions;
using Application.Abstractions.Outbound.Services;
using MediatR;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Commands.UpdateFptItem;

public class UpdateFptItemHandler : IRequestHandler<UpdateFptItemCommand, bool>
{
    private readonly IFptItemRepository _itemRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public UpdateFptItemHandler(IFptItemRepository itemRepo, IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _itemRepo = itemRepo;
        _uow = uow;
        _cloudinary = cloudinary;
    }

    public async Task<bool> Handle(UpdateFptItemCommand request, CancellationToken ct)
    {
        var item = await _itemRepo.GetByIdAsync(request.ItemId);
        if (item == null) return false;

        var imageUrl = request.ImageUrl;

        // 1. Xử lý Upload Ảnh nếu có Stream
        if (request.FileStream != null && !string.IsNullOrEmpty(request.Extension))
        {
            // Kiểm tra xem ảnh cũ có phải từ Cloudinary không để thay thế
            if (TryGetImageId(item.ImageUrl, out var existingId))
            {
                await _cloudinary.ReplaceImageAsync(existingId, request.FileStream, request.Extension, "items", ct);
                imageUrl = _cloudinary.GetImageUrl(existingId, "items");
            }
            else
            {
                var imageId = await _cloudinary.UploadImageAsync(request.FileStream, request.Extension, "items", ct);
                imageUrl = _cloudinary.GetImageUrl(imageId, "items");
            }
        }

        // 2. Chuẩn hóa ItemType
        var itemType = request.ItemType.Trim().ToLower();
        if (itemType.Contains("khung") || itemType.Contains("frame")) itemType = "avatar_frame";
        else if (itemType.Contains("nen") || itemType.Contains("bg") || itemType.Contains("background")) itemType = "background";
        else if (itemType.Contains("danhhieu") || itemType.Contains("title")) itemType = "title_color";
        else if (itemType.Contains("huyhieu") || itemType.Contains("badge")) itemType = "badge";
        else if (itemType.Contains("vatly") || itemType.Contains("physical")) itemType = "physical_item";

        item.Name = request.Name;
        item.Description = request.Description;
        item.ItemType = itemType;
        item.PriceCoin = request.PriceCoin;
        item.ImageUrl = imageUrl;
        item.DurationDays = request.DurationDays;
        item.StockQuantity = request.StockQuantity;
        item.MetaJson = request.MetaJson?.GetRawText();
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _itemRepo.UpdateAsync(item);
        await _uow.SaveChangesAsync(ct);

        return true;
    }

    private static bool TryGetImageId(string? url, out Guid imageId)
    {
        imageId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(url)) return false;
        
        // Nếu là GUID trần
        if (Guid.TryParse(url, out imageId)) return true;

        // Nếu là URL Cloudinary: .../items/{guid}
        var parts = url.Split('/');
        var lastPart = parts.LastOrDefault();
        if (lastPart != null)
        {
            var fileName = Path.GetFileNameWithoutExtension(lastPart);
            return Guid.TryParse(fileName, out imageId);
        }
        
        return false;
    }
}
