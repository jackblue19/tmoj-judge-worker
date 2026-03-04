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
/// Unit of Work wraps DbContext SaveChangesAsync.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly TmojDbContext _db;

    public EfUnitOfWork(TmojDbContext db)
        => _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}