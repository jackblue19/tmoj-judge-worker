using Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace Application.UseCases.Store.Queries.GetAdminOrders;

public class GetAdminOrdersQuery : IRequest<PagedResult<AdminOrderDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
}

public class AdminOrderDto
{
    public string Id { get; set; } = default!;
    public string ItemName { get; set; } = default!;
    public string? ItemImage { get; set; }
    public string BuyerName { get; set; } = default!;
    public string BuyerEmail { get; set; } = default!;
    public decimal Price { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = default!;
}
