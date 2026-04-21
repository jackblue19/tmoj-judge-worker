using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using MediatR;

public class GetPaymentByIdHandler
    : IRequestHandler<GetPaymentByIdQuery, GetPaymentDto>
{
    private readonly IPaymentRepository _repo;

    public GetPaymentByIdHandler(IPaymentRepository repo)
    {
        _repo = repo;
    }

    public async Task<GetPaymentDto> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await _repo.GetByIdAsync(request.PaymentId);

        if (payment == null)
            throw new Exception("Payment not found");

        return new GetPaymentDto
        {
            PaymentId = payment.PaymentId,
            Status = payment.Status,
            Amount = payment.AmountMoney
        };
    }
}