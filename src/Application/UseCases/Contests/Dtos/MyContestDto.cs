namespace Application.UseCases.Contests.Dtos;

public class MyContestDto
{
    // =========================
    // CONTEST INFO
    // =========================
    public Guid ContestId { get; set; }

    public string Title { get; set; } = default!;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    // =========================
    // TEAM INFO (FIXED)
    // =========================
    public Guid TeamId { get; set; }

    public string TeamName { get; set; } = default!;   // ✅ từ team.team_name

    public Guid LeaderId { get; set; }                 // ✅ từ team.leader_id

    // =========================
    // JOIN INFO
    // =========================
    public DateTime JoinedAt { get; set; }

    // =========================
    // RESULT (CONTEST TEAM)
    // =========================
    public int? Rank { get; set; }

    public decimal? Score { get; set; }

    public int Solved { get; set; }

    // =========================
    // 🔥 OPTIONAL (NÂNG CẤP)
    // =========================
    public string? Status { get; set; }     // upcoming | running | ended

    public string? Phase { get; set; }      // BEFORE | CODING | FINISHED

    public bool? CanJoin { get; set; }

    public bool? CanRegister { get; set; }

    public bool? CanUnregister { get; set; }

    public bool IsClosed { get; set; }
    public bool CanViewSource { get; set; }
}