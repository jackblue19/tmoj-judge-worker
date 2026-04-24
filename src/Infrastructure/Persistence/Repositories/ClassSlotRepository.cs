using Application.Common.Interfaces;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ClassSlotRepository : IClassSlotRepository
{
    private readonly TmojDbContext _db;

    public ClassSlotRepository(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ClassSemesterExistsAsync(Guid classSemesterId, CancellationToken ct = default)
    {
        return await _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct);
    }

    public async Task<bool> ProblemExistsAsync(Guid problemId, CancellationToken ct = default)
    {
        return await _db.Problems.AnyAsync(p => p.Id == problemId, ct);
    }
}
