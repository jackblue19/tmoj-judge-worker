using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Payments.Commands.CreatePayOsPayment
{
    public class CreatePayOsPaymentHandler
        : IRequestHandler<CreatePayOsPaymentCommand, CreateVnPayPaymentResponseDto>
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IPayOsService _payOsService;
        private readonly ILogger<CreatePayOsPaymentHandler> _logger;

        public CreatePayOsPaymentHandler(
            IPaymentRepository paymentRepo,
            IPayOsService payOsService,
            ILogger<CreatePayOsPaymentHandler> logger)
        {
            _paymentRepo = paymentRepo;
            _payOsService = payOsService;
            _logger = logger;
        }

        public async Task<CreateVnPayPaymentResponseDto> Handle(
            CreatePayOsPaymentCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
                throw new Exception("Amount must be greater than 0");

            var paymentId = Guid.NewGuid();

            // orderCode: strip sign bit tránh overflow, đảm bảo trong [1, 2^53-1]
            var bytes = paymentId.ToByteArray();
            var orderCode = (BitConverter.ToInt64(bytes, 0) & long.MaxValue) % 9_007_199_254_740_990L + 1;

            var payment = new Payment
            {
                PaymentId = paymentId,
                UserId = request.UserId,
                AmountMoney = request.Amount,
                Currency = "vnd",
                PaymentMethod = "payos",
                ProviderTxId = orderCode.ToString(),
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _paymentRepo.SaveChangesAsync();

            try
            {
                var checkoutUrl = await _payOsService.CreatePaymentLinkAsync(payment);

                return new CreateVnPayPaymentResponseDto
                {
                    PaymentId = payment.PaymentId,
                    PaymentUrl = checkoutUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS CreatePaymentLink ERROR | paymentId={PaymentId} | orderCode={OrderCode}",
                    payment.PaymentId, orderCode);

                // Đánh dấu payment failed nếu PayOS từ chối
                payment.Status = "failed";
                await _paymentRepo.UpdateAsync(payment);
                await _paymentRepo.SaveChangesAsync();

                throw new Exception($"PayOS error: {ex.Message}", ex);
            }
        }
    }
}
