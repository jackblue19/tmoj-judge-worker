using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Application.Common.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(Payment payment, string ipAddress)
        {
            var isFake = _config["VnPay:Fake"] == "true";

            // =========================
            // 🔥 FAKE MODE
            // =========================
            if (isFake)
            {
                var feBase = (_config["urls-fe"] ?? throw new InvalidOperationException("Missing urls-fe config")).TrimEnd('/');
                return $"{feBase}/payment-result" +
                       $"?paymentId={payment.PaymentId}" +
                       $"&status=success";
            }

            // =========================
            // REAL VNPay
            // =========================
            var vnpUrl = _config["VnPay:BaseUrl"]
                ?? throw new Exception("Missing VnPay:BaseUrl");

            var tmnCode = _config["VnPay:TmnCode"]
                ?? throw new Exception("Missing VnPay:TmnCode");

            var hashSecret = _config["VnPay:HashSecret"]
                ?? throw new Exception("Missing VnPay:HashSecret");

            var returnUrl = _config["VnPay:ReturnUrl"]
                ?? throw new Exception("Missing VnPay:ReturnUrl");

            var txnRef = payment.PaymentId.ToString();
            var amount = ((long)(payment.AmountMoney * 100)).ToString();

            var query = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", amount },
                { "vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Payment {txnRef}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", txnRef }
            };

            var queryString = string.Join("&",
                query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            var secureHash = HmacSHA512(hashSecret, queryString);

            return $"{vnpUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        // =========================
        // 🔥 VERIFY SIGNATURE (VNPay RETURN)
        // =========================
        public bool ValidateSignature(IQueryCollection query)
        {
            var isFake = _config["VnPay:Fake"] == "true";

            // Fake mode thì luôn true
            if (isFake) return true;

            var hashSecret = _config["VnPay:HashSecret"]
                ?? throw new Exception("Missing VnPay:HashSecret");

            var vnp_SecureHash = query["vnp_SecureHash"].ToString();

            // lọc param đúng chuẩn VNPay
            var filtered = query
                .Where(kvp => kvp.Key.StartsWith("vnp_") && kvp.Key != "vnp_SecureHash")
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(k => k.Key, v => v.Value.ToString());

            var rawData = string.Join("&",
                filtered.Select(kvp =>
                    $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"
                ));

            var computedHash = HmacSHA512(hashSecret, rawData);

            return computedHash.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }

        // =========================
        // HASH
        // =========================
        private string HmacSHA512(string key, string input)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(input);

            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(inputBytes);

            return BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLower();
        }
    }
}