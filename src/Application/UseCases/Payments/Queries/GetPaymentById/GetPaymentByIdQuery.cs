using MediatR;
using Application.UseCases.Payments.Dtos;

public class GetPaymentByIdQuery : IRequest<GetPaymentDto>
{
    public Guid PaymentId { get; set; }
}