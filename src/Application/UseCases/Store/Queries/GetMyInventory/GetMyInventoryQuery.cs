using Application.UseCases.Store.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Application.UseCases.Store.Queries.GetMyInventory;

public record GetMyInventoryQuery() : IRequest<List<UserInventoryDto>>;
