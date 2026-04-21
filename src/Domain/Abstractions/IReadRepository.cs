using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Abstractions;

public interface IReadRepository<TEntity, in TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id , CancellationToken ct = default);

    //  Entity specs
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec , CancellationToken ct = default);
    Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> spec , CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec , CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<TEntity> spec , CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<TEntity> spec , CancellationToken ct = default);

    // Projection specs (DTO/read model)
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default);

    Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default);

    Task<TResult?> SingleOrDefaultAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default);

    Task<PagedResult<TResult>> PageAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default);
    Task<PagedResult<TResult>> PageAsync<TResult>(
        ISpecification<TEntity> countSpec ,
        ISpecification<TEntity , TResult> listSpec ,
        int page ,
        int pageSize ,
        CancellationToken ct = default);
}
