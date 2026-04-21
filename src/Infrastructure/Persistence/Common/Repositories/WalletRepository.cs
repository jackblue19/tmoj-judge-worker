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
            return await _db.Set<Wallet>()
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task CreateAsync(Wallet wallet)
        {
            Console.WriteLine($"[WALLET] CREATE: {wallet.WalletId}");
            await _db.Set<Wallet>().AddAsync(wallet);
        }

        public Task UpdateAsync(Wallet wallet)
        {
            _db.Set<Wallet>().Update(wallet);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                Console.WriteLine("=== SAVE START ===");

                foreach (var entry in _db.ChangeTracker.Entries())
                {
                    Console.WriteLine($"STATE: {entry.Entity.GetType().Name} => {entry.State}");
                }

                var result = await _db.SaveChangesAsync();

                Console.WriteLine($"=== SAVE SUCCESS: {result} rows ===");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("=== DB UPDATE ERROR ===");

                Console.WriteLine(ex.InnerException?.Message ?? ex.Message);

                // 🔥 QUAN TRỌNG: log full entity lỗi
                foreach (var entry in ex.Entries)
                {
                    Console.WriteLine($"FAILED ENTITY: {entry.Entity.GetType().Name}");
                    Console.WriteLine($"STATE: {entry.State}");
                }

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== GENERAL ERROR ===");
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        // =========================
        // TRANSACTIONS
        // =========================
        public async Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int page, int pageSize)
        {
            return await _db.Set<WalletTransaction>()
                .Include(x => x.Wallet)
                .Where(x => x.Wallet.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddTransactionAsync(WalletTransaction transaction)
        {
            try
            {
                Console.WriteLine($"[TX] ADD START: {transaction.TransactionId}");

                await _db.Set<WalletTransaction>().AddAsync(transaction);

                Console.WriteLine($"[TX] STATE: {_db.Entry(transaction).State}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TX ADD ERROR]");
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}