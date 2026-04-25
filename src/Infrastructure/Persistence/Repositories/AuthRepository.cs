using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly TmojDbContext _db;

    public AuthRepository(TmojDbContext db) => _db = db;

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByEmailWithRoleAsync(string email, CancellationToken ct) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByEmailWithRoleAndProvidersAsync(string email, CancellationToken ct) =>
        _db.Users.Include(u => u.UserProviders).Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken ct) =>
        _db.Users.FindAsync(new object[] { userId }, ct).AsTask();

    public Task<Role?> FindRoleAsync(string code, CancellationToken ct) =>
        _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == code, ct);

    public async Task<Provider> GetOrCreateProviderAsync(string code, string displayName, CancellationToken ct)
    {
        var provider = await _db.Providers.FirstOrDefaultAsync(p => p.ProviderCode == code, ct);
        if (provider != null) return provider;

        provider = new Provider { ProviderCode = code, ProviderDisplayName = displayName, Enabled = true };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(ct);
        return provider;
    }

    public Task<EmailVerification?> FindVerificationWithUserAsync(string email, string token, CancellationToken ct) =>
        _db.EmailVerifications
            .Include(v => v.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(v => v.User.Email == email && v.Token == token, ct);

    public Task<EmailVerification?> FindVerificationAsync(string email, string token, CancellationToken ct) =>
        _db.EmailVerifications
            .FirstOrDefaultAsync(v => v.User.Email == email && v.Token == token, ct);

    public Task<List<EmailVerification>> GetVerificationsForUserAsync(Guid userId, CancellationToken ct) =>
        _db.EmailVerifications.Where(v => v.UserId == userId).ToListAsync(ct);

    public Task<RefreshToken?> FindActiveRefreshTokenAsync(string tokenHash, CancellationToken ct) =>
        _db.RefreshTokens
            .Include(t => t.Session).ThenInclude(s => s.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<List<UserSession>> GetSessionsWithTokensAsync(Guid userId, CancellationToken ct) =>
        _db.UserSessions.Where(s => s.UserId == userId).Include(s => s.RefreshTokens).ToListAsync(ct);

    public void Add(User user) => _db.Users.Add(user);
    public void AddVerification(EmailVerification v) => _db.EmailVerifications.Add(v);
    public void RemoveVerifications(IEnumerable<EmailVerification> verifications) => _db.EmailVerifications.RemoveRange(verifications);
    public void RemoveVerification(EmailVerification v) => _db.EmailVerifications.Remove(v);
    public void AddSession(UserSession session) => _db.UserSessions.Add(session);
    public void RemoveSessions(IEnumerable<UserSession> sessions) => _db.UserSessions.RemoveRange(sessions);
    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
