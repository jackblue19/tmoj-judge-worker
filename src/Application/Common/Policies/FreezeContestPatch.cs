using Domain.Entities;

namespace Application.Common.Policies;

public static class FreezeContestPatch
{
    // Freeze chỉ ảnh hưởng scoreboard public.
    // KHÔNG chặn submit, KHÔNG chặn view — contestant vẫn thao tác bình thường.
    public static bool IsFrozen(Contest contest)
    {
        var now = DateTime.UtcNow;

        if (!contest.FreezeAt.HasValue || now < contest.FreezeAt.Value)
            return false;

        if (contest.UnfreezeAt.HasValue && now >= contest.UnfreezeAt.Value)
            return false;

        return true;
    }
}