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
            var query = request.Query;

            _logger.LogInformation("VNPay callback received");

            // =========================
            // 1. Validate signature
            // =========================
            if (!_vnPayService.ValidateSignature(query))
            {
                _logger.LogWarning("Invalid signature");
                return new VnPayCallbackResult { Status = "invalid_signature" };
            }

            var txnRef = query["vnp_TxnRef"].ToString();
            var responseCode = query["vnp_ResponseCode"].ToString();

            if (string.IsNullOrEmpty(txnRef))
            {
                return new VnPayCallbackResult { Status = "missing_txn_ref" };
            }

            // =========================
            // 2. Get payment
            // =========================
            var payment = await _paymentRepo.GetByTxnRefAsync(txnRef);

            if (payment == null)
            {
                return new VnPayCallbackResult { Status = "not_found" };
            }

            _logger.LogInformation("Payment found: {PaymentId} - Status: {Status}",
                payment.PaymentId, payment.Status);

            // =========================
            // 3. SUCCESS
            // =========================
            if (responseCode == "00")
            {
                if (payment.Status != "paid")
                {
                    payment.Status = "paid";

                    await _paymentRepo.UpdateAsync(payment);
                    await _paymentRepo.SaveChangesAsync();

                    _logger.LogInformation("Payment marked as PAID");

                    // =========================
                    // WALLET
                    // =========================
                    if (payment.UserId == null)
                    {
                        _logger.LogError("Payment has no UserId");
                        return new VnPayCallbackResult
                        {
                            PaymentId = payment.PaymentId,
                            Status = "no_user"
                        };
                    }

                    var wallet = await _walletRepo.GetByUserIdAsync(payment.UserId.Value);

                    if (wallet == null)
                    {
                        _logger.LogWarning("Wallet not found -> creating");

                        wallet = new Wallet
                        {
                            WalletId = Guid.NewGuid(),
                            UserId = payment.UserId.Value,
                            Balance = 0
                        };

                        await _walletRepo.CreateAsync(wallet);
                        await _walletRepo.SaveChangesAsync();
                    }

                    // =========================
                    // 🔥 CONVERT 1:1
                    // =========================
                    var rate = int.Parse(_config["Payment:VndToCoinRate"] ?? "1");

                    var coin = (decimal)(payment.AmountMoney / rate);

                    _logger.LogInformation("Convert VND -> Coin: {Coin}", coin);

                    wallet.Balance += coin;

                    await _walletRepo.UpdateAsync(wallet);
                    await _walletRepo.SaveChangesAsync();

                    _logger.LogInformation("Wallet updated: {Balance}", wallet.Balance);

                    // =========================
                    // WALLET TRANSACTION
                    // =========================
                    var transaction = new WalletTransaction
                    {
                        TransactionId = Guid.NewGuid(),     // ✅ đúng field

                        WalletId = wallet.WalletId,

                        Type = "deposit",                   // ⚠️ theo DB constraint
                        Direction = "in",

                        Amount = coin,                     // decimal OK

                        SourceType = "payment",
                        SourceId = payment.PaymentId,

                        Status = "success",

                        CreatedAt = DateTime.UtcNow
                    };

                    await _walletRepo.AddTransactionAsync(transaction);
                    await _walletRepo.SaveChangesAsync();

                    _logger.LogInformation("Transaction created: +{Coin}", coin);
                }

                return new VnPayCallbackResult
                {
                    PaymentId = payment.PaymentId,
                    Status = "paid"
                };
            }

            // =========================
            // 4. FAILED
            // =========================
            payment.Status = "failed";

            await _paymentRepo.UpdateAsync(payment);
            await _paymentRepo.SaveChangesAsync();

            return new VnPayCallbackResult
            {
                PaymentId = payment.PaymentId,
                Status = "failed"
            };
        }
    }
}