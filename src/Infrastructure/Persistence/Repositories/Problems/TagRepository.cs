using Application.UseCases.Problems.Specifications;
using Application.UseCases.Problems;
using Domain.Abstractions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Problems;

public sealed class TagRepository : ITagRepository
{
    private readonly IReadRepository<Tag , Guid> _tagReadRepository;
    private readonly TmojDbContext _db;

    public TagRepository(
        IReadRepository<Tag , Guid> tagReadRepository ,
        TmojDbContext db)
    {
        _tagReadRepository = tagReadRepository;
        _db = db;
    }

    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds , CancellationToken ct = default)
    {
        var ids = tagIds?.Distinct().ToArray() ?? [];
        if ( ids.Length == 0 )
            return [];

        var spec = new TagsByIdsSpec(ids);
        return await _tagReadRepository.ListAsync(spec , ct);
    }

    public Task<bool> ExistsByNameAsync(string name , CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return _db.Tags.AnyAsync(x => x.Name.ToLower() == normalized , ct);
    }

    public Task<bool> ExistsBySlugAsync(string slug , CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLower();
        return _db.Tags.AnyAsync(x => x.Slug != null && x.Slug.ToLower() == normalized , ct);
    }
    public async Task<IReadOnlyList<Tag>> GetTrackedByIdsAsync(IEnumerable<Guid> tagIds , CancellationToken ct = default)
    {
        var ids = tagIds?.Where(x => x != Guid.Empty).Distinct().ToArray() ?? [];
        if ( ids.Length == 0 )
            return [];

        return await _db.Tags
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }
}