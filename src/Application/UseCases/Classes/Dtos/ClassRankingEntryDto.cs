namespace Application.UseCases.Classes.Dtos;

public record ClassRankingEntryDto(
    int Rank,
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? EquippedFrameUrl,
    int SolvedCount,
    decimal TotalScore,
    int SubmissionCount);
