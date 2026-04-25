using Application.Common.Interfaces;
using Application.UseCases.Auth.Hasher;
using MediatR;

namespace Application.UseCases.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;

    public ResetPasswordCommandHandler(IAuthRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var email = request.Email.ToLowerInvariant();
        var v = await _repo.FindVerificationAsync(email, request.Token, ct)
            ?? throw new KeyNotFoundException("Invalid or expired reset token.");

        if (v.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired reset token.");

        var user = await _repo.FindByIdAsync(v.UserId, ct);
        if (user != null)
        {
            user.Password = _hasher.Hash(request.NewPassword);
            user.EmailVerified = true;
            _repo.RemoveVerification(v);
            await _repo.SaveAsync(ct);
        }
    }
}
