using MediatR;

namespace Application.UseCases.Payments.Commands.VerifyPayOsPayment
{
    public class VerifyPayOsPaymentCommand : IRequest<VerifyPayOsPaymentResult>
    {
        public long OrderCode { get; set; }
    }

    public class VerifyPayOsPaymentResult
    {
        public string Status { get; set; } = "";
        public bool CoinsAdded { get; set; }
    }
}
