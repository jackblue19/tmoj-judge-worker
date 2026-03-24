using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Specifications;
using Application.UseCases.Problems;
using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Problems;

public sealed class ProblemRepository : IProblemRepository
{
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly TmojDbContext _db;

    public ProblemRepository(
        IReadRepository<Problem , Guid> problemReadRepository ,
        TmojDbContext db)
    {
        _problemReadRepository = problemReadRepository;
        _db = db;
    }

    public async Task<bool> SlugExistsAsync(string slug , Guid? excludingProblemId , CancellationToken ct = default)
    {
        var spec = new ProblemBySlugSpec(slug , excludingProblemId);
        return await _problemReadRepository.AnyAsync(spec , ct);
    }

    public async Task<Problem?> GetProblemForManagementAsync(
        Guid problemId ,
        Guid currentUserId ,
        bool isAdmin ,
        CancellationToken ct = default)
    {
        var query = _db.Problems
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
        return await _problemReadRepository.FirstOrDefaultAsync(spec , ct);
    }
}