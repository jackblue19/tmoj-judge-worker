using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Payments.Commands.HandlePayOsWebhook
{
    public class HandlePayOsWebhookHandler
        : IRequestHandler<HandlePayOsWebhookCommand, HandlePayOsWebhookResult>
    {
        private readonly IPayOsService _payOsService;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<HandlePayOsWebhookHandler> _logger;

        public HandlePayOsWebhookHandler(
            IPayOsService payOsService,
            IPaymentRepository paymentRepo,
            IWalletRepository walletRepo,
            IConfiguration config,
            ILogger<HandlePayOsWebhookHandler> logger)
        {
            _payOsService = payOsService;
            _paymentRepo = paymentRepo;
            _walletRepo = walletRepo;
            _config = config;
            _logger = logger;
        }

        public async Task<HandlePayOsWebhookResult> Handle(
            HandlePayOsWebhookCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // =========================
                // VERIFY SIGNATURE
                // =========================
                var verify = await _payOsService.VerifyWebhookAsync(request.Payload);

                if (!verify.IsValid)
                    return new HandlePayOsWebhookResult { Status = "invalid_signature" };

                if (!verify.IsPaid)
                    return new HandlePayOsWebhookResult { Status = "not_paid" };

                // =========================
                // TÌM PAYMENT THEO ORDER CODE
                // =========================
                var payment = await _paymentRepo.GetByProviderTxIdAsync(verify.OrderCode.ToString());

                if (payment == null)
                    return new HandlePayOsWebhookResult { Status = "not_found" };

                // Idempotency: tránh xử lý lại nếu đã paid
                if (payment.Status == "paid")
                    return new HandlePayOsWebhookResult { Status = "already_paid" };

                if (payment.UserId == null)
                    return new HandlePayOsWebhookResult { Status = "no_user" };

                // =========================
                // MARK PAYMENT PAID
                // =========================
                payment.Status = "paid";
                payment.PaidAt = DateTime.UtcNow;
                await _paymentRepo.UpdateAsync(payment);

                // =========================
                // GET OR CREATE WALLET
                // =========================
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

                // =========================
                // CONVERT MONEY → COIN
                // =========================
                decimal rate = 1m;
                if (decimal.TryParse(_config["Payment:VndToCoinRate"], out var parsedRate) && parsedRate > 0)
                    rate = parsedRate;

                var coin = payment.AmountMoney / rate;

                wallet.Balance = Math.Round(wallet.Balance + coin, 2);
                wallet.UpdatedAt = DateTime.UtcNow;

                await _walletRepo.UpdateAsync(wallet);

                // =========================
                // CREATE TRANSACTION
                // =========================
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

                return new HandlePayOsWebhookResult { Status = "paid" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS WEBHOOK ERROR");
                return new HandlePayOsWebhookResult
                {
                    Status = "error: " + (ex.InnerException?.Message ?? ex.Message)
                };
            }
        }
    }
}
