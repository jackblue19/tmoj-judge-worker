namespace Application.UseCases.Payments.Dtos
{
    public class VnPayCallbackResult
    {
        public Guid PaymentId { get; set; }

        public string Status { get; set; } = default!;
    }
}