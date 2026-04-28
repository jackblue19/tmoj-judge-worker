using System;

namespace Application.UseCases.Store.Dtos;

public class FptItemDto
{
    public Guid ItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ItemType { get; set; } = null!;
    public decimal PriceCoin { get; set; }
    public string? ImageUrl { get; set; }
    public int? DurationDays { get; set; }
    public int StockQuantity { get; set; }
    public System.Text.Json.JsonElement? MetaJson { get; set; }
}

public class UserInventoryDto
{
    public Guid InventoryId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = null!;
    public string? ItemImageUrl { get; set; }
    public string ItemType { get; set; } = null!;
    public DateTime AcquiredAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int Quantity { get; set; }
    public bool IsEquipped { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}
