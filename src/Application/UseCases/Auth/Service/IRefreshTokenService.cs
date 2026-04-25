namespace Application.UseCases.Auth.Service;

public interface IRefreshTokenService
{
    string GenerateToken();
    string HashToken(string token);
}
