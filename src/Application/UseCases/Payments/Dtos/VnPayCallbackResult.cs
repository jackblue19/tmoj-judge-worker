namespace Application.UseCases.Payments.Dtos;

    public class VnPayCallbackResult
    {
        public Guid? PaymentId { get; set; }
        public string Status { get; set; } = default!;
        public bool? WalletUpdated { get; set; }
        public bool? TransactionCreated { get; set; }
    }
