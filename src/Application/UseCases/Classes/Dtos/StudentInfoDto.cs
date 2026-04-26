namespace Application.UseCases.Classes.Dtos;

public record StudentInfoDto(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Email,
    string? AvatarUrl,
    DateTime JoinedAt,
    bool IsActive,
    int SubmissionCount,
    int SolvedCount,
    DateTime? LastSubmissionAt);
