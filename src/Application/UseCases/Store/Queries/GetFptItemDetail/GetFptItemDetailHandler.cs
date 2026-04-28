using Application.Common.Interfaces;
using Application.UseCases.Store.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Queries.GetFptItemDetail;

public class GetFptItemDetailHandler : IRequestHandler<GetFptItemDetailQuery, FptItemDto?>
{
    private readonly IFptItemRepository _itemRepo;

    public GetFptItemDetailHandler(IFptItemRepository itemRepo)
    {
        _itemRepo = itemRepo;
    }

    public async Task<FptItemDto?> Handle(GetFptItemDetailQuery request, CancellationToken ct)
    {
        var item = await _itemRepo.GetByIdAsync(request.ItemId);
        if (item == null) return null;

        return new FptItemDto
        {
            ItemId = item.ItemId,
            Name = item.Name,
            Description = item.Description,
            ItemType = item.ItemType,
            PriceCoin = item.PriceCoin,
            ImageUrl = item.ImageUrl,
            DurationDays = item.DurationDays,
            MetaJson = SafeParseJson(item.MetaJson)
        };
    }

    private System.Text.Json.JsonElement? SafeParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }
}
