using System;
using System.Collections.Generic;

namespace WebAPI.Controllers.v1.Auth;

public record UserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? Username,
    string? AvatarUrl,
    bool emailVerified, 
    List<string> Roles);

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

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto User,
    string TokenType = "Bearer");
