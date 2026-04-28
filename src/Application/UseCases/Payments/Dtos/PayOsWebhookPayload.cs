using System.Text.Json.Serialization;

namespace Application.UseCases.Payments.Dtos
{
    public class PayOsWebhookPayload
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public PayOsWebhookPayloadData? Data { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = "";
    }

    public class PayOsWebhookPayloadData
    {
        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = "";

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = "";

        [JsonPropertyName("transactionDateTime")]
        public string TransactionDateTime { get; set; } = "";

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "";

        [JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; } = "";

        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        [JsonPropertyName("counterAccountBankId")]
        public string CounterAccountBankId { get; set; } = "";

        [JsonPropertyName("counterAccountBankName")]
        public string CounterAccountBankName { get; set; } = "";

        [JsonPropertyName("counterAccountName")]
        public string CounterAccountName { get; set; } = "";

        [JsonPropertyName("counterAccountNumber")]
        public string CounterAccountNumber { get; set; } = "";

        [JsonPropertyName("virtualAccountName")]
        public string VirtualAccountName { get; set; } = "";

        [JsonPropertyName("virtualAccountNumber")]
        public string VirtualAccountNumber { get; set; } = "";
    }

    public class PayOsVerifyResult
    {
        public bool IsValid { get; set; }
        public bool IsPaid { get; set; }
        public long OrderCode { get; set; }
    }
}
