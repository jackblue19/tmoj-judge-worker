using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Payments.Commands.CreateVnPayPayment
{
    public class CreateVnPayPaymentHandler
        : IRequestHandler<CreateVnPayPaymentCommand, CreateVnPayPaymentResponseDto>
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IVnPayService _vnPayService;

        public CreateVnPayPaymentHandler(
            IPaymentRepository paymentRepo,
            IVnPayService vnPayService)
        {
            _paymentRepo = paymentRepo;
            _vnPayService = vnPayService;
        }

        public async Task<CreateVnPayPaymentResponseDto> Handle(
            CreateVnPayPaymentCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
                throw new Exception("Amount must be greater than 0");

            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = request.UserId,
                AmountMoney = request.Amount,
                Currency = "vnd",
                PaymentMethod = "vnpay",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);

            var paymentUrl = _vnPayService.CreatePaymentUrl(payment, request.IpAddress);

            return new CreateVnPayPaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                PaymentUrl = paymentUrl
            };
        }
    }
}