using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories
{
    public class EditorialRepository : IEditorialRepository
    {
        private readonly TmojDbContext _db;

        public EditorialRepository(TmojDbContext db)
        {
            _db = db;
        }

        public async Task<Editorial?> GetByIdAsync(Guid id)
        {
            return await _db.Editorials
                .FirstOrDefaultAsync(x => x.EditorialId == id);
        }

        public async Task<List<Editorial>> GetByProblemIdAsync(Guid problemId)
        {
            return await _db.Editorials
                .Where(x => x.ProblemId == problemId)
                .OrderByDescending(x => x.CreatedAt) // 🔥 luôn sort
                .ToListAsync();
        }

        public async Task<Guid> CreateAsync(Editorial editorial)
        {
            await _db.Editorials.AddAsync(editorial);
            await _db.SaveChangesAsync();

            return editorial.EditorialId;
        }

        public void Update(Editorial editorial)
        {
            _db.Editorials.Update(editorial);
        }

        public void Delete(Editorial editorial)
        {
            _db.Editorials.Remove(editorial);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}