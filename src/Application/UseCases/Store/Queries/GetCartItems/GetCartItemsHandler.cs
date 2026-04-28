using Application.Common.Interfaces;
using Application.UseCases.Store.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Queries.GetCartItems;

public class GetCartItemsHandler : IRequestHandler<GetCartItemsQuery, List<CartItemDto>>
{
    private readonly ICartItemRepository _cartRepo;
    private readonly ICurrentUserService _currentUser;

    public GetCartItemsHandler(ICartItemRepository cartRepo, ICurrentUserService currentUser)
    {
        _cartRepo = cartRepo;
        _currentUser = currentUser;
    }

    public async Task<List<CartItemDto>> Handle(GetCartItemsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId == Guid.Empty)
            return new List<CartItemDto>();

        var items = await _cartRepo.GetByUserIdAsync(userId.Value);

        return items.Select(x => new CartItemDto
        {
            CartItemId = x.CartItemId,
            ItemId = x.ItemId,
            Name = x.Item.Name,
            ImageUrl = x.Item.ImageUrl,
            PriceCoin = x.Item.PriceCoin,
            Quantity = x.Quantity
        }).ToList();
    }
}
