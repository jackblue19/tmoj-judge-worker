using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Application.Common.Locks;

public static class ReportProcessingLock
{
    private static readonly ConcurrentDictionary<Guid, byte> _locks = new();

    public static bool TryAcquire(Guid reportId)
        => _locks.TryAdd(reportId, 0);

    public static void Release(Guid reportId)
        => _locks.TryRemove(reportId, out _);
}