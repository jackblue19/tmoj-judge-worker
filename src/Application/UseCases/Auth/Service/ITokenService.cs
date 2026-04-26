namespace Application.UseCases.Auth.Service;

/// <summary>
/// Này là sample cho Tuấn
/// đó thích thì dựa trên base này ko thì code kiểu khác cũng được nhưng cần phải chuẩn
/// yêu cầu cần có cả access token và refresh token
/// </summary>

public interface ITokenService
{
    string CreateAccessToken(
        string userId,
        string? userName,
        IEnumerable<string> roles,
        IDictionary<string, string>? extraClaims = null);
}
