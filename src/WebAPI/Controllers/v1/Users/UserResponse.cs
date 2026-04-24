using System;
using Application.UseCases.Classes.Dtos;

namespace WebAPI.Controllers.v1.Users;

public record UserProfileResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? Username,
    string? AvatarUrl,
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
    bool emailVerified,
    bool Status,
    string? Role);

public record SimpleUserDto(
    Guid UserId,
    string? DisplayName,
    string Email,
    string? AvatarUrl);

public record StudentProfileWithClassesResponse(
    UserDto Student,
    List<ClassInstanceDto> Classes,
    int TotalClasses);

public record TeacherSubjectInfo(
    Guid SubjectId,
    string Code,
    string Name,
    string? Description,
    int ClassCount);

public record TeacherDetailResponse(
    UserDto Teacher,
    List<TeacherSubjectInfo> Subjects,
    List<ClassInstanceDto> Classes,
    int TotalClasses);
