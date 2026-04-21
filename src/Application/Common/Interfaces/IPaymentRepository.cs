using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(Guid id);
        Task UpdateAsync(Payment payment);
    }
}