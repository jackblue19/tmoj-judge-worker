using Application.UseCases.Store.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Application.UseCases.Store.Queries.GetCartItems;

public record GetCartItemsQuery : IRequest<List<CartItemDto>>;
