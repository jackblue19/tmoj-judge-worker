using Application.UseCases.Problems.Specifications;
using Application.UseCases.Problems;
using Domain.Abstractions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories.Problems;

public sealed class TagRepository : ITagRepository
{
    private readonly IReadRepository<Tag , Guid> _tagReadRepository;

    public TagRepository(IReadRepository<Tag , Guid> tagReadRepository)
    {
        _tagReadRepository = tagReadRepository;
    }

    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds , CancellationToken ct = default)
    {
        var ids = tagIds?.Distinct().ToArray() ?? [];
        if ( ids.Length == 0 )
            return [];

        var spec = new TagsByIdsSpec(ids);
        return await _tagReadRepository.ListAsync(spec , ct);
    }
}
