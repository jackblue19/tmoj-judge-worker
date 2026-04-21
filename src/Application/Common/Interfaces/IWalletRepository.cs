using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByUserIdAsync(Guid userId);

        Task CreateAsync(Wallet wallet);
        Task UpdateAsync(Wallet wallet);
        Task SaveChangesAsync();

        // =========================
        // WALLET TRANSACTIONS (MERGED)
        // =========================
        Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int page, int pageSize);
        Task AddTransactionAsync(WalletTransaction transaction);
    }
}