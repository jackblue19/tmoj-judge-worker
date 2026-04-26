namespace Application.UseCases.Classes.Dtos;

public record InviteCodeStatusDto(
    Guid InstanceId,
    string? ClassCode,
    string? SemesterCode,
    string? SubjectCode,
    string? InviteCode,
    DateTime? ExpiresAt,
    bool IsActive,
    double? RemainingSeconds);
