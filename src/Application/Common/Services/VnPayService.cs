using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

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
            var vnpUrl = _config["VnPay:BaseUrl"]!;
            var tmnCode = _config["VnPay:TmnCode"]!;
            var hashSecret = _config["VnPay:HashSecret"]!;
            var returnUrl = _config["VnPay:ReturnUrl"]!;

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
                { "vnp_TxnRef", txnRef },
                { "vnp_ExpireDate", DateTime.UtcNow.AddMinutes(15).ToString("yyyyMMddHHmmss") }
            };

            var queryString = string.Join("&",
                query.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

            var secureHash = HmacSHA512(hashSecret, queryString);

            return $"{vnpUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(IQueryCollection query)
        {
            var hashSecret = _config["VnPay:HashSecret"]!;
            var vnpSecureHash = query["vnp_SecureHash"].ToString();

            var filtered = query
                .Where(x => x.Key.StartsWith("vnp_") && x.Key != "vnp_SecureHash")
                .OrderBy(x => x.Key);

            var rawData = string.Join("&",
                filtered.Select(x => $"{x.Key}={x.Value}"));

            var computedHash = HmacSHA512(hashSecret, rawData);

            return computedHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSHA512(string key, string input)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(input);

            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(inputBytes);

            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}