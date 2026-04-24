namespace Application.UseCases.Classes.Dtos;

public record ClassContestDto(
    Guid ContestId,
    Guid ClassId,
    Guid? SlotId,
    string Title,
    string? Slug,
    string? DescriptionMd,
    string? Rules,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? FreezeAt,
    bool IsActive,
    bool IsJoined,
    double? TimeRemainingSeconds,
    DateTime CreatedAt,
    List<ContestProblemDto> Problems);
