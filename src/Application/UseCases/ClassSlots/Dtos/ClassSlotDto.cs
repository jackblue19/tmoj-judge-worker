namespace Application.UseCases.ClassSlots.Dtos;

public record ClassSlotDto(
    Guid Id,
    Guid ClassId,
    int SlotNo,
    string Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    string Mode,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<SlotProblemDto> Problems);

public record SlotProblemDto(
    Guid ProblemId,
    string? ProblemTitle,
    string? ProblemSlug,
    int? Ordinal,
    int? Points,
    bool IsRequired);

public record StudentSlotScoreDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    List<ProblemScoreDto> ProblemScores,
    decimal TotalScore,
    int SolvedCount);

public record ProblemScoreDto(
    Guid ProblemId,
    string? ProblemTitle,
    string? VerdictCode,
    decimal? Score,
    int Attempts,
    DateTime? LastSubmittedAt);

public record StudentSubmissionDetailDto(
    Guid SubmissionId,
    Guid ProblemId,
    string? ProblemTitle,
    string? VerdictCode,
    decimal? FinalScore,
    int? TimeMs,
    int? MemoryKb,
    string? StatusCode,
    DateTime CreatedAt,
    List<SubmissionResultDto> Results);

public record SubmissionResultDto(
    Guid ResultId,
    string? StatusCode,
    int? RuntimeMs,
    int? MemoryKb,
    string? CheckerMessage,
    string? Input,
    string? ExpectedOutput,
    string? ActualOutput);

public record ClassSlotRankingDto(
    Guid ClassSlotId,
    string SlotTitle,
    string? SlotDescription,
    DateTime? DueAt,
    List<StudentRankingDto> Rankings,
    DateTime LastUpdated);

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
