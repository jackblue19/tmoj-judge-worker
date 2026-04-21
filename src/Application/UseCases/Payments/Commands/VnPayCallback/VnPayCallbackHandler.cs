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

                _logger.LogInformation("=== VNPay CALLBACK START ===");

                foreach (var q in query)
                {
                    _logger.LogInformation("[VNPay] {Key} = {Value}", q.Key, q.Value.ToString());
                }

                // =========================
                // VALIDATE SIGNATURE
                // =========================
                if (!_vnPayService.ValidateSignature(query))
                {
                    return new VnPayCallbackResult
                    {
                        Status = "invalid_signature"
                    };
                }

                var txnRef = query["vnp_TxnRef"].ToString();
                var responseCode = query["vnp_ResponseCode"].ToString();

                if (string.IsNullOrEmpty(txnRef))
                {
                    return new VnPayCallbackResult
                    {
                        Status = "missing_txn_ref"
                    };
                }

                // =========================
                // FIND PAYMENT
                // =========================
                var payment = await _paymentRepo.GetByTxnRefAsync(txnRef);

                if (payment == null)
                {
                    _logger.LogError("Payment NOT FOUND: {TxnRef}", txnRef);

                    return new VnPayCallbackResult
                    {
                        Status = "not_found"
                    };
                }

                // =========================
                // SUCCESS FLOW
                // =========================
                if (responseCode == "00")
                {
                    if (payment.Status == "paid")
                    {
                        return new VnPayCallbackResult
                        {
                            PaymentId = payment.PaymentId,
                            Status = "already_processed"
                        };
                    }

                    // mark paid
                    payment.Status = "paid";

                    if (payment.UserId == null)
                    {
                        return new VnPayCallbackResult
                        {
                            Status = "no_user"
                        };
                    }

                    var wallet = await _walletRepo.GetByUserIdAsync(payment.UserId.Value);

                    if (wallet == null)
                    {
                        wallet = new Wallet
                        {
                            WalletId = Guid.NewGuid(),
                            UserId = payment.UserId.Value,
                            Balance = 0,
                            Currency = "VND",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _walletRepo.CreateAsync(wallet);
                    }

                    // rate safe
                    decimal rate = 1m;
                    decimal.TryParse(_config["Payment:VndToCoinRate"], out rate);

                    var coin = payment.AmountMoney / rate;

                    wallet.Balance = Math.Round(wallet.Balance + coin, 2);
                    wallet.UpdatedAt = DateTime.UtcNow;

                    // =========================
                    // TRANSACTION
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

                        CreatedAt = DateTime.UtcNow,
                        Wallet = wallet
                    };

                    _logger.LogInformation("[TX] CREATE {Id}", transaction.TransactionId);

                    // =========================
                    // SAVE ALL IN 1 TIME (IMPORTANT FIX)
                    // =========================
                    await _paymentRepo.UpdateAsync(payment);
                    await _walletRepo.UpdateAsync(wallet);

                    await _walletRepo.AddTransactionAsync(transaction);

                    await _walletRepo.SaveChangesAsync();

                    _logger.LogInformation("[TX] SAVED SUCCESS");

                    return new VnPayCallbackResult
                    {
                        PaymentId = payment.PaymentId,
                        Status = "paid",
                        WalletUpdated = true,
                        TransactionCreated = true
                    };
                }

                // =========================
                // FAILED FLOW
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay CALLBACK CRASH");

                return new VnPayCallbackResult
                {
                    Status = "error: " + (ex.InnerException?.Message ?? ex.Message)
                };
            }
        }
    }
}