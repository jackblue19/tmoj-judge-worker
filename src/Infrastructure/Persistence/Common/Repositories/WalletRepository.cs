using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly TmojDbContext _db;

        public WalletRepository(TmojDbContext db)
        {
            _db = db;
        }

        // =========================
        // WALLET
        // =========================
        public async Task<Wallet?> GetByUserIdAsync(Guid userId)
        {
            var wallet = await _db.Set<Wallet>()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            Console.WriteLine($"[WalletRepo] GetByUserId: {userId} => {(wallet != null ? wallet.WalletId.ToString() : "NULL")}");

            return wallet;
        }

        public async Task CreateAsync(Wallet wallet)
        {
            Console.WriteLine($"[WalletRepo] Create wallet for UserId: {wallet.UserId}");
            await _db.Set<Wallet>().AddAsync(wallet);
        }

        public Task UpdateAsync(Wallet wallet)
        {
            Console.WriteLine($"[WalletRepo] Update wallet: {wallet.WalletId} balance={wallet.Balance}");
            _db.Set<Wallet>().Update(wallet);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
            Console.WriteLine("[WalletRepo] SaveChanges executed");
        }

        // =========================
        // WALLET TRANSACTION (MERGED)
        // =========================
        public async Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int page, int pageSize)
        {
            Console.WriteLine($"[WalletRepo] GetTransactions userId={userId}, page={page}");

            var data = await _db.Set<WalletTransaction>()
                .Include(x => x.Wallet)
                .Where(x => x.Wallet.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Console.WriteLine($"[WalletRepo] Transactions found: {data.Count}");

            return data;
        }

        // ✅ ADD THIS
        public async Task AddTransactionAsync(WalletTransaction transaction)
        {
            Console.WriteLine($"[WalletRepo] AddTransaction type={transaction.Type}, amount={transaction.Amount}");

            await _db.Set<WalletTransaction>().AddAsync(transaction);
        }
    }
}