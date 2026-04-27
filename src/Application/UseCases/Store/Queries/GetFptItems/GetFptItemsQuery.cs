using Application.UseCases.Store.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Application.UseCases.Store.Queries.GetFptItems;

public record GetFptItemsQuery() : IRequest<List<FptItemDto>>;
