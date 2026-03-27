using Domain.Entities;

namespace Application.UseCases.Problems;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> GetTrackedByIdsAsync(IEnumerable<Guid> tagIds, CancellationToken ct = default);
}
