using System;
using System.Collections.Generic;

namespace WebAPI.Controllers.v1.Auth;

// Request models
public record CreateAccountRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public record ConfirmEmailRequest(string Email, string Token);

public record GoogleLoginRequest(string TokenId);

public record GithubLoginRequest(string AccessToken);

public record LoginRequest(string Email, string Password);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record RefreshTokenRequest(string RefreshToken);

public record AssignRoleRequest(string RoleCode);
