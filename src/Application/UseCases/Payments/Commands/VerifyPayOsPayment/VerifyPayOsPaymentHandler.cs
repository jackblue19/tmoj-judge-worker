using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Payments.Commands.VerifyPayOsPayment
{
    public class VerifyPayOsPaymentHandler
        : IRequestHandler<VerifyPayOsPaymentCommand, VerifyPayOsPaymentResult>
    {
        private readonly IPayOsService _payOsService;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<VerifyPayOsPaymentHandler> _logger;

        public VerifyPayOsPaymentHandler(
            IPayOsService payOsService,
            IPaymentRepository paymentRepo,
            IWalletRepository walletRepo,
            IConfiguration config,
            ILogger<VerifyPayOsPaymentHandler> logger)
        {
            _payOsService = payOsService;
            _paymentRepo = paymentRepo;
            _walletRepo = walletRepo;
            _config = config;
            _logger = logger;
        }

        public async Task<VerifyPayOsPaymentResult> Handle(
            VerifyPayOsPaymentCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var payment = await _paymentRepo.GetByProviderTxIdAsync(request.OrderCode.ToString());

                if (payment == null)
                    return new VerifyPayOsPaymentResult { Status = "not_found" };

                // Idempotency: đã xử lý rồi thì trả về luôn
                if (payment.Status == "paid")
                    return new VerifyPayOsPaymentResult { Status = "paid", CoinsAdded = false };

                // Hỏi PayOS xem đã thanh toán chưa
                var isPaid = await _payOsService.IsPaymentPaidAsync(request.OrderCode);

                if (!isPaid)
                    return new VerifyPayOsPaymentResult { Status = "pending" };

                if (payment.UserId == null)
                    return new VerifyPayOsPaymentResult { Status = "no_user" };

                // Mark paid
                payment.Status = "paid";
                payment.PaidAt = DateTime.UtcNow;
                await _paymentRepo.UpdateAsync(payment);

                // Get or create wallet
                var wallet = await _walletRepo.GetByUserIdAsync(payment.UserId.Value);
                if (wallet == null)
                {
                    wallet = new Wallet
                    {
                        WalletId = Guid.NewGuid(),
                        UserId = payment.UserId.Value,
                        Balance = 0,
                        Currency = "coin",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _walletRepo.CreateAsync(wallet);
                    await _walletRepo.SaveChangesAsync();
                }

                // Convert money → coin
                decimal rate = 1m;
                if (decimal.TryParse(_config["Payment:VndToCoinRate"], out var parsedRate) && parsedRate > 0)
                    rate = parsedRate;
                var coin = payment.AmountMoney / rate;

                wallet.Balance = Math.Round(wallet.Balance + coin, 2);
                wallet.UpdatedAt = DateTime.UtcNow;
                await _walletRepo.UpdateAsync(wallet);

                var transaction = new WalletTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    Type = "deposit",
                    Direction = "in",
                    Amount = Math.Abs(coin),
                    SourceType = "payos",
                    SourceId = payment.PaymentId,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                };
                await _walletRepo.AddTransactionAsync(transaction);

                await _walletRepo.SaveChangesAsync();
                await _paymentRepo.SaveChangesAsync();

                return new VerifyPayOsPaymentResult { Status = "paid", CoinsAdded = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyPayOsPayment ERROR | orderCode={OrderCode}", request.OrderCode);
                return new VerifyPayOsPaymentResult { Status = "error" };
            }
        }
    }
}
