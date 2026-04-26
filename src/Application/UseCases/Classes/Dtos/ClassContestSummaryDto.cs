namespace Application.UseCases.Classes.Dtos;

public record ClassContestSummaryDto(
    Guid ContestId,
    string Title,
    string? Slug,
    DateTime StartAt,
    DateTime EndAt,
    bool IsActive,
    int ProblemCount,
    int ParticipantCount);
