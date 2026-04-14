using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Specifications;
using Application.UseCases.Problems;
using Domain.Abstractions;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence.Common;

namespace Infrastructure.Persistence.Repositories.Problems;

public sealed class ProblemRepository : EfRepository<Problem , Guid>, IProblemRepository
{
    public ProblemRepository(TmojDbContext db) : base(db)
    {
    }

    public async Task<bool> SlugExistsAsync(string slug , Guid? excludingProblemId , CancellationToken ct = default)
    {
        var spec = new ProblemBySlugSpec(slug , excludingProblemId);
        return await AnyAsync(spec , ct);
    }

    public async Task<Problem?> GetProblemForManagementAsync(
        Guid problemId ,
        Guid currentUserId ,
        bool isAdmin ,
        CancellationToken ct = default)
    {
        var query = _set
            .Include(x => x.Tags)
            .Where(x => x.Id == problemId);

        if ( !isAdmin )
        {
            query = query.Where(x => x.CreatedBy == currentUserId);
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<ProblemDetailDto?> GetProblemDetailForManagementAsync(
        Guid problemId ,
        Guid currentUserId ,
        bool isAdmin ,
        CancellationToken ct = default)
    {
        var exists = await _db.Problems
            .Where(x => x.Id == problemId)
            .Where(x => isAdmin || x.CreatedBy == currentUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if ( exists == Guid.Empty )
            return null;

        var spec = new ProblemDetailForManagementSpec(problemId);
        return await FirstOrDefaultAsync(spec , ct);
    }

    public async Task<Problem?> GetProblemTrackedWithTagsAsync(Guid problemId , CancellationToken ct = default)
    {
        return await _db.Problems
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == problemId , ct);
    }

    public async Task<Problem?> GetProblemTrackedWithTagsAndTestsetsAsync(Guid problemId , CancellationToken ct = default)
    {
        return await _db.Problems
            .Include(x => x.Tags)
            .Include(x => x.Testsets)
            .FirstOrDefaultAsync(x => x.Id == problemId , ct);
    }

    public async Task<IReadOnlyList<Tag>> GetTagsTrackedByIdsAsync(
    IReadOnlyCollection<Guid> tagIds ,
    CancellationToken ct = default)
    {
        if ( tagIds.Count == 0 )
            return [];

        var normalized = tagIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        return await _db.Tags
            .Where(x => normalized.Contains(x.Id) && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }
}