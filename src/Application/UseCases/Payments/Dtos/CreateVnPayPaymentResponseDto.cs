namespace Application.UseCases.Payments.Dtos
{
    public class CreateVnPayPaymentResponseDto
    {
        public Guid PaymentId { get; set; }
        public string PaymentUrl { get; set; } = default!;
    }
}