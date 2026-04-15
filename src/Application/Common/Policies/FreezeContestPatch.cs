using Domain.Entities;

namespace Application.Common.Policies;

public static class FreezeContestPatch
{
    public static bool IsFrozen(Contest contest)
    {
        return contest.FreezeAt.HasValue
            && DateTime.UtcNow >= contest.FreezeAt.Value;
    }

    public static void EnsureViewAllowed(Contest contest)
    {
        if (IsFrozen(contest))
            throw new Exception("CONTEST_FROZEN_VIEW_BLOCKED");
    }

    public static void EnsureSubmitAllowed(Contest contest)
    {
        if (IsFrozen(contest))
            throw new Exception("CONTEST_FROZEN_SUBMIT_BLOCKED");
    }
}