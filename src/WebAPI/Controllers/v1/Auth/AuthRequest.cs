using System;

namespace WebAPI.Controllers.v1.Auth;

// Request models
public record CreateAccountRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public record ConfirmEmailRequest(string Email, string Token);

public record GoogleLoginRequest(string TokenId);

public record LoginRequest(string Email, string Password);
