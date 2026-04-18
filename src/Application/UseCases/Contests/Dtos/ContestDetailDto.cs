namespace Application.UseCases.Contests.Dtos;

public class ContestDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Slug { get; set; } = "";

    public string Visibility { get; set; } = "";
    public string ContestType { get; set; } = "";
    public bool AllowTeams { get; set; }

    public string? InviteCode { get; set; }

    public string Status { get; set; } = "";
    public string Phase { get; set; } = "";

    public bool IsPublished { get; set; }

    // =========================
    // 🔥 FREEZE FEATURE
    // =========================
    public bool IsFrozen { get; set; }
    public DateTime? FreezeAt { get; set; }

    // =========================
    // 🔥 VIEW CONTROL FLAGS (FIX MISSING ERROR)
    // =========================
    public bool CanViewProblems { get; set; }
    public bool CanViewDetail { get; set; }

    public bool CanJoin { get; set; }
    public bool IsRegistered { get; set; }
    public bool HasLeaderboard { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int DurationMinutes { get; set; }

    public int ProblemCount { get; set; }
    public int TotalPoints { get; set; }

    public List<ContestProblemDto> Problems { get; set; } = new();
}