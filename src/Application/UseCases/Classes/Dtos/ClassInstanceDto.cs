namespace Application.UseCases.Classes.Dtos;

public record ClassInstanceDto(
    Guid ClassSemesterId,
    string ClassCode,
    Guid SemesterId,
    string SemesterCode,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string? SubjectDescription,
    DateOnly StartAt,
    DateOnly EndAt,
    string? InviteCode,
    DateTime? InviteCodeExpiresAt,
    DateTime CreatedAt,
    ClassTeacherDto? Teacher,
    int MemberCount);
