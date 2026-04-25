namespace Application.UseCases.Auth.Hasher;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
