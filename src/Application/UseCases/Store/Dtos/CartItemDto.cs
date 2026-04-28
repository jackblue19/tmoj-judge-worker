using System;

namespace Application.UseCases.Store.Dtos;

public class CartItemDto
{
    public Guid CartItemId { get; set; }
    public Guid ItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public decimal PriceCoin { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => PriceCoin * Quantity;
}
