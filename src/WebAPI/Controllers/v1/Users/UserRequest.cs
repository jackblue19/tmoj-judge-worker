using System;

namespace WebAPI.Controllers.v1.Users;

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? AvatarUrl);

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Username,
    List<string>? Roles);

