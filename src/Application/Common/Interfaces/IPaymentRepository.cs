using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(Guid id);
        Task UpdateAsync(Payment payment);

        Task<Payment?> GetByTxnRefAsync(string txnRef);
        Task<bool> ExistsAsync(Guid paymentId);

        Task SaveChangesAsync(); // 🔥 bắt buộc
    }
}