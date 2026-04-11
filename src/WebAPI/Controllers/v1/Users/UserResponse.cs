using System;

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
    string? Role);
