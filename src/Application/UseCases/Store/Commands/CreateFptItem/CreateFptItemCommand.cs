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
    string? MetaJson
) : IRequest<Guid>;
