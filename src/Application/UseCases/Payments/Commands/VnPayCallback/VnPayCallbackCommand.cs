using Application.UseCases.Payments.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Payments.Commands.VnPayCallback
{
    public class VnPayCallbackCommand : IRequest<VnPayCallbackResult>
    {
        public IQueryCollection Query { get; set; } = default!;
    }
}