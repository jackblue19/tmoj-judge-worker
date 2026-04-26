namespace Application.UseCases.Contests.Commands;

public class JoinByCodeResult
{
    /// <summary>"team" khi dùng team invite code, "contest" khi dùng contest invite code</summary>
    public string Type { get; set; } = "";
    public Guid TeamId { get; set; }
    public Guid? ContestTeamId { get; set; }
    public Guid? ContestId { get; set; }
}
