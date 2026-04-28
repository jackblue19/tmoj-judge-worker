using Application.UseCases.Payments.Dtos;
using MediatR;

namespace Application.UseCases.Payments.Commands.HandlePayOsWebhook
{
    public class HandlePayOsWebhookCommand : IRequest<HandlePayOsWebhookResult>
    {
        public PayOsWebhookPayload Payload { get; set; } = default!;
    }

    public class HandlePayOsWebhookResult
    {
        public string Status { get; set; } = "";
    }
}
