namespace Application.UseCases.Auth.Dtos;

public record AuthUserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? Username,
    string? AvatarUrl,
    bool EmailVerified,
    string? Role);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    AuthUserDto User,
    string TokenType = "Bearer");
