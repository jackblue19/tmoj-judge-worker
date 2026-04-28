using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment?> GetByTxnRefAsync(string txnRef);
        Task<bool> ExistsAsync(Guid paymentId);
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
        
        Task<Payment?> GetByProviderTxIdAsync(string providerTxId);

        Task<(List<Payment> Items, int TotalItems)> GetMyPaymentHistoryAsync(Guid userId, int page, int pageSize);
        Task<(List<Payment> Items, int TotalItems)> GetAllPaymentHistoryAsync(int page, int pageSize);
    }
}