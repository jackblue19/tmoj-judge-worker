using Application.Common.Helpers;

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

    public string Status { get; set; } = "";
    public string Phase { get; set; } = ""; // ✅ NEW

    public bool IsPublished { get; set; }

    public bool CanJoin { get; set; } // ✅ NEW
    public bool IsRegistered { get; set; } // ✅ NEW

    public bool HasLeaderboard { get; set; } // ✅ NEW

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int DurationMinutes { get; set; }

    public int ProblemCount { get; set; }
    public int TotalPoints { get; set; }

    public List<ContestProblemDto> Problems { get; set; } = new();
}