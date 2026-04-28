using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace Infrastructure.Services
{
    public class PayOsService : IPayOsService
    {
        private readonly PayOSClient _client;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;

        private const long MaxSafeOrderCode = 9_007_199_254_740_991L;

        public PayOsService(IConfiguration config)
        {
            var clientId = config["PayOS:ClientId"]
                ?? throw new Exception("Missing PayOS:ClientId");
            var apiKey = config["PayOS:APIKey"]
                ?? throw new Exception("Missing PayOS:APIKey");
            var checksumKey = config["PayOS:ChecksumKey"]
                ?? throw new Exception("Missing PayOS:ChecksumKey");

            var feUrl = (config["urls-fe"] ?? throw new Exception("Missing urls-fe config")).TrimEnd('/');
            _returnUrl = config["PayOS:ReturnUrl"] ?? $"{feUrl}/payment-result";
            _cancelUrl = config["PayOS:CancelUrl"] ?? $"{feUrl}/payment-cancel";

            _client = new PayOSClient(clientId, apiKey, checksumKey);
        }

        public async Task<string> CreatePaymentLinkAsync(Payment payment)
        {
            var orderCode = GenerateOrderCode(payment.PaymentId);

            var request = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (long)payment.AmountMoney,
                Description = "NapCoinTMOJ",
                ReturnUrl = _returnUrl,
                CancelUrl = _cancelUrl
            };

            var result = await _client.PaymentRequests.CreateAsync(request);
            return result.CheckoutUrl;
        }

        public async Task<bool> IsPaymentPaidAsync(long orderCode)
        {
            try
            {
                var link = await _client.PaymentRequests.GetAsync(orderCode);
                return link.Status == PayOS.Models.V2.PaymentRequests.PaymentLinkStatus.Paid;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PayOsVerifyResult> VerifyWebhookAsync(PayOsWebhookPayload payload)
        {
            try
            {
                var webhook = new Webhook
                {
                    Code = payload.Code,
                    Description = payload.Desc,
                    Success = payload.Success,
                    Signature = payload.Signature,
                    Data = payload.Data == null ? new WebhookData() : new WebhookData
                    {
                        OrderCode = payload.Data.OrderCode,
                        Amount = payload.Data.Amount,
                        Description = payload.Data.Description,
                        AccountNumber = payload.Data.AccountNumber,
                        Reference = payload.Data.Reference,
                        TransactionDateTime = payload.Data.TransactionDateTime,
                        Currency = payload.Data.Currency,
                        PaymentLinkId = payload.Data.PaymentLinkId,
                        Code = payload.Data.Code,
                        Description2 = payload.Data.Desc,
                        CounterAccountBankId = payload.Data.CounterAccountBankId,
                        CounterAccountBankName = payload.Data.CounterAccountBankName,
                        CounterAccountName = payload.Data.CounterAccountName,
                        CounterAccountNumber = payload.Data.CounterAccountNumber,
                        VirtualAccountName = payload.Data.VirtualAccountName,
                        VirtualAccountNumber = payload.Data.VirtualAccountNumber
                    }
                };

                var data = await _client.Webhooks.VerifyAsync(webhook);

                return new PayOsVerifyResult
                {
                    IsValid = true,
                    IsPaid = data.Code == "00",
                    OrderCode = data.OrderCode
                };
            }
            catch (WebhookException)
            {
                return new PayOsVerifyResult { IsValid = false };
            }
        }

        // Tạo orderCode dương duy nhất từ GUID, trong phạm vi 1..MaxSafeOrderCode
        // Dùng & long.MaxValue để strip sign bit, tránh overflow của Math.Abs(long.MinValue)
        private static long GenerateOrderCode(Guid paymentId)
        {
            var bytes = paymentId.ToByteArray();
            var raw = BitConverter.ToInt64(bytes, 0) & long.MaxValue; // luôn >= 0
            return (raw % (MaxSafeOrderCode - 1)) + 1;               // luôn trong [1, MaxSafeOrderCode-1]
        }
    }
}
