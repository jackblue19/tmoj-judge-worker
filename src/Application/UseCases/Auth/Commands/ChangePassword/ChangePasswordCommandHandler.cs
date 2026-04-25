using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Auth.Hasher;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Application.UseCases.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, string>
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public ChangePasswordCommandHandler(
        IAuthRepository repo, IPasswordHasher hasher, IEmailService email, IConfiguration config)
    {
        _repo = repo;
        _hasher = hasher;
        _email = email;
        _config = config;
    }

    public async Task<string> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _repo.FindByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrEmpty(user.Password) || !_hasher.Verify(request.CurrentPassword, user.Password))
            throw new UnauthorizedAccessException("Invalid current password.");

        user.Password = _hasher.Hash(request.NewPassword);
        user.EmailVerified = false;

        var old = await _repo.GetVerificationsForUserAsync(user.UserId, ct);
        _repo.RemoveVerifications(old);

        var v = new EmailVerification
        {
            UserId = user.UserId,
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _repo.AddVerification(v);
        await _repo.SaveAsync(ct);

        var confirmLink = $"https://localhost:7210/api/v1/Auth/confirm-email?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(v.Token)}";
        var template = _config.GetSection("EmailSettings")["ChangePasswordEmailTemplate"]
                       ?? "<a href='{LINK}'>Xác nhận Email</a>";
        var body = template.Replace("{LINK}", confirmLink).Replace("{YEAR}", DateTime.Now.Year.ToString());
        await _email.SendEmailAsync(user.Email, "Xác nhận địa chỉ email - TMOJ", body, ct);

        return v.Token;
    }
}
