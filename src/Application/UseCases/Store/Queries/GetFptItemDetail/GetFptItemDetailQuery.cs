using Application.UseCases.Store.Dtos;
using MediatR;
using System;

namespace Application.UseCases.Store.Queries.GetFptItemDetail;

public record GetFptItemDetailQuery(Guid ItemId) : IRequest<FptItemDto?>;
