using Application.UseCases.Payments.Dtos;
using MediatR;

namespace Application.UseCases.Payments.Commands.CreateVnPayPayment
{
    public class CreateVnPayPaymentCommand : IRequest<CreateVnPayPaymentResponseDto>
    {
        public decimal Amount { get; set; }
        public Guid UserId { get; set; }
        public string IpAddress { get; set; } = default!;
    }
}