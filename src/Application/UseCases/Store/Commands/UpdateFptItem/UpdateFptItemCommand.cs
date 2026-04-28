using MediatR;
using System;

namespace Application.UseCases.Store.Commands.UpdateFptItem;

public record UpdateFptItemCommand(
    Guid ItemId,
    string Name,
    string? Description,
    string ItemType,
    decimal PriceCoin,
    string? ImageUrl,
    int? DurationDays,
    int StockQuantity,
    System.Text.Json.JsonElement? MetaJson,
    bool IsActive
) : IRequest<bool>;
