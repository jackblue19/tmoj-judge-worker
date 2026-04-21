namespace Application.UseCases.Payments.Dtos
{
    public class GetPaymentDetailResult
    {
        public string Status { get; set; } = default!;
        public decimal Amount { get; set; }
    }
}