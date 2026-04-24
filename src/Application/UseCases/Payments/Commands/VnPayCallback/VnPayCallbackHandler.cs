using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Payments.Commands.VnPayCallback
{
    public class VnPayCallbackHandler
        : IRequestHandler<VnPayCallbackCommand, VnPayCallbackResult>
    {
        private readonly IVnPayService _vnPayService;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<VnPayCallbackHandler> _logger;

        public VnPayCallbackHandler(
            IVnPayService vnPayService,
            IPaymentRepository paymentRepo,
            IWalletRepository walletRepo,
            IConfiguration config,
            ILogger<VnPayCallbackHandler> logger)
        {
            _vnPayService = vnPayService;
            _paymentRepo = paymentRepo;
            _walletRepo = walletRepo;
            _config = config;
            _logger = logger;
        }

        public async Task<VnPayCallbackResult> Handle(
            VnPayCallbackCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var query = request.Query;

                if (!_vnPayService.ValidateSignature(query))
                {
                    return new VnPayCallbackResult { Status = "invalid_signature" };
                }

                var txnRef = query["vnp_TxnRef"].ToString();
                var responseCode = query["vnp_ResponseCode"].ToString();

                var payment = await _paymentRepo.GetByTxnRefAsync(txnRef);

                if (payment == null)
                    return new VnPayCallbackResult { Status = "not_found" };

                if (responseCode != "00")
                {
                    payment.Status = "failed";
                    await _paymentRepo.UpdateAsync(payment);
                    await _paymentRepo.SaveChangesAsync();

                    return new VnPayCallbackResult { Status = "failed" };
                }

                // =========================
                // MARK PAYMENT PAID
                // =========================
                payment.Status = "paid";
                await _paymentRepo.UpdateAsync(payment);

                if (payment.UserId == null)
                    return new VnPayCallbackResult { Status = "no_user" };

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

                    // 🔥 MUST SAVE FIRST TO AVOID FK ERROR
                    await _walletRepo.SaveChangesAsync();
                }

                // =========================
                // CONVERT MONEY → COIN
                // =========================
                decimal rate = 1000m;
                decimal.TryParse(_config["Payment:VndToCoinRate"], out rate);

                var coin = payment.AmountMoney / rate;

                wallet.Balance = Math.Round(wallet.Balance + coin, 2);
                wallet.UpdatedAt = DateTime.UtcNow;

                await _walletRepo.UpdateAsync(wallet);

                // =========================
                // CREATE TRANSACTION (FIXED FK ISSUE)
                // =========================
                var transaction = new WalletTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,

                    Type = "deposit",
                    Direction = "in",
                    Amount = Math.Abs(coin),

                    SourceType = "vnpay",
                    SourceId = payment.PaymentId,

                    Status = "completed",
                    CreatedAt = DateTime.UtcNow

                    // ❌ DO NOT SET Wallet navigation
                };

                await _walletRepo.AddTransactionAsync(transaction);

                // =========================
                // FINAL SAVE (ALL CHANGES)
                // =========================
                await _walletRepo.SaveChangesAsync();
                await _paymentRepo.SaveChangesAsync();

                return new VnPayCallbackResult
                {
                    PaymentId = payment.PaymentId,
                    Status = "paid",
                    WalletUpdated = true,
                    TransactionCreated = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay CALLBACK ERROR");

                return new VnPayCallbackResult
                {
                    Status = "error: " + (ex.InnerException?.Message ?? ex.Message)
                };
            }
        }
    }
}