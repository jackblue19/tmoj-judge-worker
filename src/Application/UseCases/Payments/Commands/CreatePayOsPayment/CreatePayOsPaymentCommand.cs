using Application.UseCases.Payments.Dtos;
using MediatR;

namespace Application.UseCases.Payments.Commands.CreatePayOsPayment
{
    public class CreatePayOsPaymentCommand : IRequest<CreateVnPayPaymentResponseDto>
    {
        public decimal Amount { get; set; }
        public Guid UserId { get; set; }
    }
}
