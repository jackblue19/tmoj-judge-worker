using MediatR;

namespace Application.UseCases.ClassSlots.Queries;

public class GetClassSlotRankingsQuery : IRequest<GetClassSlotRankingsResponse>
{
    public Guid ClassSlotId { get; set; }
}

public class GetClassSlotRankingsResponse
{
    public Guid ClassSlotId { get; set; }
    public string SlotTitle { get; set; } = string.Empty;
    public string? SlotDescription { get; set; }
    public DateTime? DueAt { get; set; }
    public List<StudentRankingDto> Rankings { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class StudentRankingDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rank { get; set; }
    public int Solved { get; set; }
    public int Penalty { get; set; }
    public List<ProblemRankingDto> Problems { get; set; } = new();
}

public class ProblemRankingDto
{
    public Guid ProblemId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsSolved { get; set; }
    public int Attempts { get; set; }
    public int? PenaltyTime { get; set; }
}
