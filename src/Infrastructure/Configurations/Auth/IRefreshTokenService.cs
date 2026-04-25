using Application.UseCases.Auth.Service;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Configurations.Auth;

// Interface moved to Application.UseCases.Auth.IRefreshTokenService
public sealed class RefreshTokenService : IRefreshTokenService
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
