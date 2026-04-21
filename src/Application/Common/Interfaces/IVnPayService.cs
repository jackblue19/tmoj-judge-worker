using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(Payment payment, string ipAddress);
    }
}