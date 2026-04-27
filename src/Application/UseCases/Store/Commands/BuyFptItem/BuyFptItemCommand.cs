using MediatR;
using System;

namespace Application.UseCases.Store.Commands.BuyFptItem;

public record BuyFptItemCommand(Guid ItemId) : IRequest<Guid>;
