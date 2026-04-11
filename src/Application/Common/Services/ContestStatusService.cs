using Application.Common.Interfaces;

namespace Application.Common.Services;

public class ContestStatusService : IContestStatusService
{
    public string GetStatus(DateTime startAt, DateTime endAt)
    {
        var now = DateTime.UtcNow;

        if (now < startAt)
            return "upcoming";

        if (now > endAt)
            return "ended";

        return "running";
    }

    public string GetPhase(DateTime startAt, DateTime endAt)
    {
        var now = DateTime.UtcNow;

        if (now < startAt)
            return "BEFORE";

        if (now > endAt)
            return "FINISHED";

        return "CODING";
    }

    public bool CanJoin(DateTime startAt, DateTime endAt)
    {
        var now = DateTime.UtcNow;

  
        return now <= endAt;
    }
}