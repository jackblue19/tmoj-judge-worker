using MediatR;
using System;

namespace Application.UseCases.Store.Commands.AddToCart;

public record AddToCartCommand(Guid ItemId, int Quantity) : IRequest<bool>;
