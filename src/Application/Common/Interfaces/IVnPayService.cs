using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Common.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(Payment payment, string ipAddress);

        bool ValidateSignature(IQueryCollection query);
    }
}