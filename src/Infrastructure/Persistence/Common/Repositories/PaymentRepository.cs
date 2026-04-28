using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly TmojDbContext _db;

        public PaymentRepository(TmojDbContext db)
        {
            _db = db;
        }

        // =========================
        // CREATE
        // =========================
        public async Task AddAsync(Payment payment)
        {
            await _db.Set<Payment>().AddAsync(payment);
        }

        // =========================
        // READ
        // =========================
        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _db.Set<Payment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PaymentId == id);
        }

        public async Task<Payment?> GetByTxnRefAsync(string txnRef)
        {
            return await _db.Set<Payment>()
                .FirstOrDefaultAsync(x => x.PaymentId.ToString() == txnRef);
        }

        public async Task<Payment?> GetByProviderTxIdAsync(string providerTxId)
        {
            return await _db.Set<Payment>()
                .FirstOrDefaultAsync(x => x.ProviderTxId == providerTxId);
        }

        public async Task<bool> ExistsAsync(Guid paymentId)
        {
            return await _db.Set<Payment>()
                .AnyAsync(x => x.PaymentId == paymentId);
        }

        // =========================
        // UPDATE
        // =========================
        public Task UpdateAsync(Payment payment)
        {
            _db.Set<Payment>().Update(payment);
            return Task.CompletedTask;
        }

        // =========================
        // SAVE
        // =========================
        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        // =========================
        // HISTORY
        // =========================
        public async Task<(List<Payment> Items, int TotalItems)> GetMyPaymentHistoryAsync(Guid userId, int page, int pageSize)
        {
            var query = _db.Set<Payment>().AsNoTracking().Where(x => x.UserId == userId);
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalItems);
        }

        public async Task<(List<Payment> Items, int TotalItems)> GetAllPaymentHistoryAsync(int page, int pageSize)
        {
            var query = _db.Set<Payment>().AsNoTracking();
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalItems);
        }
    }
}