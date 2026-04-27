using Application.UseCases.Store.Dtos;
using MediatR;
using System;

namespace Application.UseCases.Store.Queries.GetUserInventoryDetail;

public record GetUserInventoryDetailQuery(Guid InventoryId) : IRequest<UserInventoryDto?>;
