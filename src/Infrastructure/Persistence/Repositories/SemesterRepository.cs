using Application.Common.Interfaces;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SemesterRepository : ISemesterRepository
{
    private readonly TmojDbContext _db;

    public SemesterRepository(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasActiveClassesAsync(Guid semesterId, CancellationToken ct = default)
    {
        return await _db.ClassSemesters
            .AnyAsync(cs => cs.SemesterId == semesterId && cs.Class.IsActive, ct);
    }
}
