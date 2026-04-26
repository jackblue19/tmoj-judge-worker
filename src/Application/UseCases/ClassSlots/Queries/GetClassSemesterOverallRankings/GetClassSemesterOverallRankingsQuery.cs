using MediatR;

namespace Application.UseCases.ClassSlots.Queries;

public class GetClassSemesterOverallRankingsQuery : IRequest<GetClassSemesterOverallRankingsResponse>
{
    public Guid ClassSemesterId { get; set; }
}

public class GetClassSemesterOverallRankingsResponse
{
    public Guid ClassSemesterId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public List<ClassSlotOverviewDto> Slots { get; set; } = new();
    public List<StudentOverallRankingDto> Rankings { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ClassSlotOverviewDto
{
    public Guid SlotId { get; set; }
    public int SlotNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DueAt { get; set; }
}

public class StudentOverallRankingDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rank { get; set; }

    // Overall stats (tổng toàn bộ slots)
    public int TotalSolved { get; set; }
    public int TotalPenalty { get; set; }

    // Per-slot breakdown
    public List<StudentSlotStatsDto> SlotStats { get; set; } = new();
}

public class StudentSlotStatsDto
{
    public Guid SlotId { get; set; }
    public string SlotTitle { get; set; } = string.Empty;
    public int Solved { get; set; }
    public int Penalty { get; set; }
}
