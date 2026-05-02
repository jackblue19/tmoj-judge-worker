using Application.UseCases.Classes.Dtos;

namespace Application.UseCases.Users.Dtos;

public record UserProfileDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? Username,
    string? AvatarUrl,
    string? EquippedFrameUrl,
    bool EmailVerified,
    bool Status,
    DateTime CreatedAt);

public record UserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? Username,
    string? RollNumber,
    string? MemberCode,
    string? AvatarUrl,
    string? EquippedFrameUrl,
    bool EmailVerified,
    bool Status,
    string? Role);

public record SimpleUserDto(
    Guid UserId,
    string? DisplayName,
    string Email,
    string? AvatarUrl,
    string? EquippedFrameUrl);

public record StudentProfileWithClassesDto(
    UserDto Student,
    List<ClassInstanceDto> Classes,
    int TotalClasses);

public record TeacherSubjectInfoDto(
    Guid SubjectId,
    string Code,
    string Name,
    string? Description,
    int ClassCount);

public record TeacherDetailDto(
    UserDto Teacher,
    List<TeacherSubjectInfoDto> Subjects,
    List<ClassInstanceDto> Classes,
    int TotalClasses);

public record ImportStudentItem(
    string FullName,
    string Email,
    string? RollNumber,
    string? MemberCode);
