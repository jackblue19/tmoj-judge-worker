using Application.UseCases.Payments.Dtos;
using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IPayOsService
    {
        Task<string> CreatePaymentLinkAsync(Payment payment);
        Task<PayOsVerifyResult> VerifyWebhookAsync(PayOsWebhookPayload payload);
        Task<bool> IsPaymentPaidAsync(long orderCode);
    }
}
