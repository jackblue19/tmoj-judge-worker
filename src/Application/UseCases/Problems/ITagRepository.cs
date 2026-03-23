using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds , CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name , CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug , CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> GetTrackedByIdsAsync(IEnumerable<Guid> tagIds , CancellationToken ct = default);
}
