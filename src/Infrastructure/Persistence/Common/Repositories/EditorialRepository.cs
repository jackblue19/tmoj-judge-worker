using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<List<Editorial>> GetByProblemIdAsync(Guid problemId)
        {
            return await _db.Editorials
                .Where(x => x.ProblemId == problemId)
                .ToListAsync();
        }

        public async Task<Editorial?> GetByIdAsync(Guid id)
        {
            return await _db.Editorials.FindAsync(id);
        }

        public async Task AddAsync(Editorial editorial)
        {
            await _db.Editorials.AddAsync(editorial);
        }

        public void Update(Editorial editorial)
        {
            _db.Editorials.Update(editorial);
        }

        public void Remove(Editorial editorial)
        {
            _db.Editorials.Remove(editorial);
        }
    }
}
