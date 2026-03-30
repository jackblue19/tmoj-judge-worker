using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Domain.Abstractions;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common;

/// <summary>
/// - Base on EfReadRepository, EfWriteRepository
/// EF Core read repository using Ardalis.Specification.
/// - No IQueryable leaks
/// - NoTracking by default (read-safe)
/// - Spec-driven querying
/// Write -> luôn impl ko có savechanges -> muốn saves -> dùng UOW
/// </summary>
public class EfRepository<TEntity, Tkey> : IReadRepository<TEntity , Tkey>, IWriteRepository<TEntity , Tkey>
    where TEntity : class
{
    protected readonly TmojDbContext _db;
    protected readonly DbSet<TEntity> _set;
    protected readonly ISpecificationEvaluator _evaluator;
    protected readonly bool _defaultNoTracking;

    // Cached PK metadata (single-column only)
    protected readonly string? _singlePkName;
    protected readonly Type? _singlePkClrType;

    /// <summary>
    /// -   protected cho phép lớp con (extend/impl) => tái sử dụng variables
    /// -   private => only current class
    /// </summary>
    /// <param name="db"></param>
    /// <param name="evaluator"></param>
    /// <param name="defaultNoTracking"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public EfRepository(
        TmojDbContext db ,
        ISpecificationEvaluator? evaluator = null ,
        bool defaultNoTracking = true)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _set = _db.Set<TEntity>();
        _evaluator = evaluator ?? SpecificationEvaluator.Default;
        _defaultNoTracking = defaultNoTracking;

        var entityType = _db.Model.FindEntityType(typeof(TEntity));
        var pk = entityType?.FindPrimaryKey();

        if ( pk is not null && pk.Properties.Count == 1 )
        {
            var prop = pk.Properties[0];
            _singlePkName = prop.Name;
            _singlePkClrType = prop.ClrType;
        }
    }

    #region READ REPOSITORY

    /// <summary>
    /// Get entity by primary key.
    /// - Prefer query-by-PK + NoTracking
    /// - Composite key => fallback FindAsync (may return tracked entity)
    /// - PK type mismatch => throw (fail fast)
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(Tkey id , CancellationToken ct = default)
    {
        if ( _singlePkName is null || _singlePkClrType is null )
        {
            // Composite key or unknown PK
            // EF FindAsync may return tracked instance
            return await _set.FindAsync(new object?[] { id } , ct);
        }

        var pkType = Nullable.GetUnderlyingType(_singlePkClrType) ?? _singlePkClrType;
        var keyType = Nullable.GetUnderlyingType(typeof(Tkey)) ?? typeof(Tkey);

        if ( pkType != keyType )
        {
            throw new InvalidOperationException(
                $"TKey type '{typeof(Tkey).Name}' does not match PK type '{pkType.Name}' for entity '{typeof(TEntity).Name}'.");
        }

        var query = BaseQuery();
        var predicate = BuildPkPredicate(_singlePkName , id);

        return await query.FirstOrDefaultAsync(predicate , ct);
    }

    public virtual Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec , CancellationToken ct = default)
        => ApplySpec(spec).FirstOrDefaultAsync(ct);

    public virtual Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> spec , CancellationToken ct = default)
        => ApplySpec(spec).SingleOrDefaultAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec , CancellationToken ct = default)
        => await ApplySpec(spec).ToListAsync(ct);

    public virtual Task<int> CountAsync(ISpecification<TEntity> spec , CancellationToken ct = default)
        => ApplySpec(spec , evaluateCriteriaOnly: true).CountAsync(ct);

    public virtual Task<bool> AnyAsync(ISpecification<TEntity> spec , CancellationToken ct = default)
        => ApplySpec(spec , evaluateCriteriaOnly: true).AnyAsync(ct);

    public virtual async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default)
        => await ApplySpec(spec).ToListAsync(ct);

    public virtual Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default)
        => ApplySpec(spec).FirstOrDefaultAsync(ct);

    public virtual Task<TResult?> SingleOrDefaultAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default)
        => ApplySpec(spec).SingleOrDefaultAsync(ct);

    /// <summary>
    /// Convenience paging.
    /// Requires: if Skip &gt; 0 then Take must be specified.
    /// </summary>
    public virtual async Task<PagedResult<TResult>> PageAsync<TResult>(
        ISpecification<TEntity , TResult> spec ,
        CancellationToken ct = default)
    {
        if ( spec.Skip.HasValue && !spec.Take.HasValue )
            throw new InvalidOperationException(
                "Paging requires both Skip and Take to be specified.");

        var items = await ApplySpec(spec).ToListAsync(ct);

        var countSpec = BuildCountSpecFromProjection(spec);
        var total = await CountAsync(countSpec , ct);

        int page, pageSize;

        if ( spec.Take is int take && take > 0 )
        {
            pageSize = take;
            var skip = spec.Skip ?? 0;
            page = skip / pageSize + 1;
        }
        else
        {
            page = 1;
            pageSize = items.Count;
        }

        return new PagedResult<TResult>
        {
            Items = items ,
            Page = page ,
            PageSize = pageSize ,
            TotalCount = total
        };
    }

    /// <summary>
    /// Enterprise-safe paging: caller controls count & list specs explicitly.
    /// </summary>
    public virtual async Task<PagedResult<TResult>> PageAsync<TResult>(
        ISpecification<TEntity> countSpec ,
        ISpecification<TEntity , TResult> listSpec ,
        int page ,
        int pageSize ,
        CancellationToken ct = default)
    {
        if ( page < 1 ) page = 1;
        if ( pageSize < 1 ) pageSize = 1;

        var total = await CountAsync(countSpec , ct);
        var items = await ApplySpec(listSpec).ToListAsync(ct);

        return new PagedResult<TResult>
        {
            Items = items ,
            Page = page ,
            PageSize = pageSize ,
            TotalCount = total
        };
    }

    // ===== Internals =====

    private IQueryable<TEntity> BaseQuery()
    {
        var q = _set.AsQueryable();
        return _defaultNoTracking ? q.AsNoTracking() : q;
    }

    private IQueryable<TEntity> ApplySpec(
        ISpecification<TEntity> spec ,
        bool evaluateCriteriaOnly = false)
        => _evaluator.GetQuery(BaseQuery() , spec , evaluateCriteriaOnly);

    private IQueryable<TResult> ApplySpec<TResult>(
        ISpecification<TEntity , TResult> spec)
        => _evaluator.GetQuery(BaseQuery() , spec);

    private static Expression<Func<TEntity , bool>> BuildPkPredicate(string pkName , Tkey id)
    {
        var param = Expression.Parameter(typeof(TEntity) , "e");

        var efProperty = Expression.Call(
            typeof(EF) ,
            nameof(EF.Property) ,
            new[] { typeof(Tkey) } ,
            param ,
            Expression.Constant(pkName));

        var equals = Expression.Equal(
            efProperty ,
            Expression.Constant(id , typeof(Tkey)));

        return Expression.Lambda<Func<TEntity , bool>>(equals , param);
    }

    private static ISpecification<TEntity> BuildCountSpecFromProjection<TResult>(
        ISpecification<TEntity , TResult> projectionSpec)
        => new CountOnlySpec<TResult>(projectionSpec);

    /// <summary>
    /// Count-only spec: clones WHERE + SEARCH only.
    /// </summary>
    private sealed class CountOnlySpec<TRes> : Specification<TEntity>
    {
        public CountOnlySpec(ISpecification<TEntity , TRes> src)
        {
            foreach ( var where in src.WhereExpressions )
                Query.Where(where.Filter);

            foreach ( var search in src.SearchCriterias )
                Query.Search(
                    search.Selector ,
                    search.SearchTerm ,
                    search.SearchGroup);
        }
    }

    #endregion


    #region WRITE REPOSITORY

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

    public virtual async Task RemoveByIdAsync(Tkey id , CancellationToken ct = default)
    {
        var entity = await _set.FindAsync(new object?[] { id } , ct);
        if ( entity is null ) return;
        _set.Remove(entity);
    }

    #endregion

}
