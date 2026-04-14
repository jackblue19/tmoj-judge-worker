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

    // 🔥 FIX: phải nằm trong thời gian contest
    public bool CanJoin(DateTime startAt, DateTime endAt)
    {
        var now = DateTime.UtcNow;

        return now >= startAt && now <= endAt;
    }

    // 🔥 REGISTER: trước 8 tiếng
    public bool CanRegister(DateTime startAt)
        => DateTime.UtcNow < startAt.AddHours(-8);

    public bool CanUnregister(DateTime startAt)
        => DateTime.UtcNow < startAt.AddHours(-4);

    // 🔥 UPDATE: sửa theo logic của mày
    public bool CanModifyTeam(DateTime startAt)
        => DateTime.UtcNow < startAt.AddHours(-4);
}