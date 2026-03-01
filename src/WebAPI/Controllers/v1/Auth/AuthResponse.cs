namespace WebAPI.Controllers.v1.Auth;

public record UserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? Username,
    string? AvatarUrl,
    List<string> Roles);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto User);
