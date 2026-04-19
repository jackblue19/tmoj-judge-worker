namespace Application.UseCases.Contests.Dtos;

public class ContestStatusDto
{
    // draft / scheduled / running / closed / finalized / cancelled
    public string ContestStatus { get; set; } = "draft";

    // live / frozen / final / hidden
    public string ScoreboardMode { get; set; } = "live";

    public DateTime ServerTime { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime? FreezeAt { get; set; }
    public DateTime? UnfreezeAt { get; set; }
    public DateTime? FinalizedAt { get; set; }

    public bool CanSubmit { get; set; }
    public bool CanViewProblems { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsFinalized { get; set; }
}
