namespace Application.UseCases.Auth.Dtos;

public record GoogleLoginRequest(string TokenId);
public record GithubLoginRequest(string AccessToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record RefreshTokenRequest(string RefreshToken);
