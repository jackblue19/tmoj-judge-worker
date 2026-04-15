using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class ProblemEditorialRepository : IProblemEditorialRepository
{
    private readonly TmojDbContext _db;

    public ProblemEditorialRepository(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<ProblemEditorial?> GetByIdAsync(Guid id)
    {
        return await _db.ProblemEditorials
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<ProblemEditorial>> GetByProblemIdAsync(Guid problemId, int take)
    {
        return await _db.ProblemEditorials
            .Where(x => x.ProblemId == problemId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ProblemEditorial entity)
    {
        try
        {
            await _db.ProblemEditorials.AddAsync(entity);
            await _db.SaveChangesAsync();

            return entity.Id;
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine("==== DB UPDATE ERROR ====");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.InnerException?.Message); // 🔥 quan trọng nhất
            Console.WriteLine("=========================");

            throw; // để controller bắt tiếp
        }
    }

    public void Update(ProblemEditorial entity)
    {
        _db.ProblemEditorials.Update(entity);
    }

    public void Delete(ProblemEditorial entity)
    {
        _db.ProblemEditorials.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine("==== DB SAVE ERROR ====");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.InnerException?.Message);
            Console.WriteLine("======================");

            throw;
        }
    }
}