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

namespace Infrastructure.Persistence.Repositories.Problems;

public sealed class ProblemRepository : IProblemRepository
{
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;

    public ProblemRepository(IReadRepository<Problem , Guid> problemReadRepository)
    {
        _problemReadRepository = problemReadRepository;
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
        var spec = new ProblemForManagementSpec(problemId);
        var entity = await _problemReadRepository.FirstOrDefaultAsync(spec , ct);

        if ( entity is null )
            return null;

        if ( isAdmin )
            return entity;

        return entity.CreatedBy == currentUserId ? entity : null;
    }

    public async Task<ProblemDetailDto?> GetProblemDetailForManagementAsync(
        Guid problemId ,
        Guid currentUserId ,
        bool isAdmin ,
        CancellationToken ct = default)
    {
        var entity = await GetProblemForManagementAsync(problemId , currentUserId , isAdmin , ct);
        if ( entity is null )
            return null;

        var spec = new ProblemDetailForManagementSpec(problemId);
        return await _problemReadRepository.FirstOrDefaultAsync(spec , ct);
    }
}
