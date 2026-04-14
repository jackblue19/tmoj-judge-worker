namespace Application.Common.Interfaces;

public interface IContestStatusService
{
    string GetStatus(DateTime startAt, DateTime endAt);

    string GetPhase(DateTime startAt, DateTime endAt);

    bool CanJoin(DateTime startAt, DateTime endAt);

    bool CanRegister(DateTime startAt);

    bool CanUnregister(DateTime startAt);

    bool CanModifyTeam(DateTime startAt);
}