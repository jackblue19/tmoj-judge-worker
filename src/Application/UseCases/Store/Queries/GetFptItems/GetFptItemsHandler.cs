using Application.Common.Interfaces;
using Application.UseCases.Store.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Queries.GetFptItems;

public class GetFptItemsHandler : IRequestHandler<GetFptItemsQuery, List<FptItemDto>>
{
    private readonly IFptItemRepository _itemRepo;

    public GetFptItemsHandler(IFptItemRepository itemRepo)
    {
        _itemRepo = itemRepo;
    }

    public async Task<List<FptItemDto>> Handle(GetFptItemsQuery request, CancellationToken ct)
    {
        var items = await _itemRepo.GetAllActiveAsync();
        
        return items.Select(x => new FptItemDto
        {
            ItemId = x.ItemId,
            Name = x.Name,
            Description = x.Description,
            ItemType = x.ItemType,
            PriceCoin = x.PriceCoin,
            ImageUrl = x.ImageUrl,
            DurationDays = x.DurationDays,
            MetaJson = x.MetaJson
        }).ToList();
    }
}
