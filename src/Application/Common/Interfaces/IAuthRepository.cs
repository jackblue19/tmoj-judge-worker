using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IAuthRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindByEmailWithRoleAsync(string email, CancellationToken ct);
    Task<User?> FindByEmailWithRoleAndProvidersAsync(string email, CancellationToken ct);
    Task<User?> FindByIdAsync(Guid userId, CancellationToken ct);
    Task<Role?> FindRoleAsync(string code, CancellationToken ct);
    Task<Provider> GetOrCreateProviderAsync(string code, string displayName, CancellationToken ct);
    Task<EmailVerification?> FindVerificationWithUserAsync(string email, string token, CancellationToken ct);
    Task<EmailVerification?> FindVerificationAsync(string email, string token, CancellationToken ct);
    Task<List<EmailVerification>> GetVerificationsForUserAsync(Guid userId, CancellationToken ct);
    Task<RefreshToken?> FindActiveRefreshTokenAsync(string tokenHash, CancellationToken ct);
    Task<List<UserSession>> GetSessionsWithTokensAsync(Guid userId, CancellationToken ct);
    void Add(User user);
    void AddVerification(EmailVerification v);
    void RemoveVerifications(IEnumerable<EmailVerification> verifications);
    void RemoveVerification(EmailVerification v);
    void AddSession(UserSession session);
    void RemoveSessions(IEnumerable<UserSession> sessions);
    Task SaveAsync(CancellationToken ct);
}
