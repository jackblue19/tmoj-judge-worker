using System;

namespace WebAPI.Controllers.v1.Users;

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? AvatarUrl);

/// <summary>Admin updates any user — cannot change Email, MemberCode, RollNumber.</summary>
public record AdminUpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? Username,
    string? DisplayName,
    string? Password,
    string? RoleCode,
    bool? Status);

/// <summary>User updates own profile — cannot change Email, Username, Role, RollNumber, MemberCode.</summary>
public record UpdateMyProfileRequest(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Password);

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Username,
    List<string>? Roles);

public record UpdateRoleRequest(string RoleCode);
public record AssignRoleRequest(string RoleCode);
