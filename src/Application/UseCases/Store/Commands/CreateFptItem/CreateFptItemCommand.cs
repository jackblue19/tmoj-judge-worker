using MediatR;
using System;

namespace Application.UseCases.Store.Commands.CreateFptItem;

public record CreateFptItemCommand(
    string Name,
    string? Description,
    string ItemType,
    decimal PriceCoin,
    string? ImageUrl,
    int? DurationDays,
    int StockQuantity,
    System.Text.Json.JsonElement? MetaJson
) : IRequest<Guid>;
