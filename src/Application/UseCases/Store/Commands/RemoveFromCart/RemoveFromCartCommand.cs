using MediatR;
using System;

namespace Application.UseCases.Store.Commands.RemoveFromCart;

public record RemoveFromCartCommand(Guid CartItemId) : IRequest<bool>;
