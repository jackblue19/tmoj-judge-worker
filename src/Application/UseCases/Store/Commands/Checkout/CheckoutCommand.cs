using MediatR;

namespace Application.UseCases.Store.Commands.Checkout;

public record CheckoutCommand : IRequest<bool>;
