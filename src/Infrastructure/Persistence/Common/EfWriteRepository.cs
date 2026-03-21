using Domain.Abstractions;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common;

/// <summary>
/// EF Core write repository.
/// Write -> luôn impl ko có savechanges -> muốn saves -> dùng UOW
/// </summary>
public class EfWriteRepository<TEntity, TKey> : IWriteRepository<TEntity , TKey>
    where TEntity : class
{
    private readonly TmojDbContext _db;
    private readonly DbSet<TEntity> _set;

    public EfWriteRepository(TmojDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _set = _db.Set<TEntity>();
    }

    public virtual async Task AddAsync(TEntity entity , CancellationToken ct = default)
    {
        if ( entity is null ) throw new ArgumentNullException(nameof(entity));
        await _set.AddAsync(entity , ct);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities , CancellationToken ct = default)
    {
        if ( entities is null ) throw new ArgumentNullException(nameof(entities));
        await _set.AddRangeAsync(entities , ct);
    }

    public virtual void Update(TEntity entity)
    {
        if ( entity is null ) throw new ArgumentNullException(nameof(entity));
        _set.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        if ( entities is null ) throw new ArgumentNullException(nameof(entities));
        _set.UpdateRange(entities);
    }

    public virtual void Remove(TEntity entity)
    {
        if ( entity is null ) throw new ArgumentNullException(nameof(entity));
        _set.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        if ( entities is null ) throw new ArgumentNullException(nameof(entities));
        _set.RemoveRange(entities);
    }

    public virtual async Task RemoveByIdAsync(TKey id , CancellationToken ct = default)
    {
        var entity = await _set.FindAsync(new object?[] { id } , ct);
        if ( entity is null ) return;
        _set.Remove(entity);
    }
}
