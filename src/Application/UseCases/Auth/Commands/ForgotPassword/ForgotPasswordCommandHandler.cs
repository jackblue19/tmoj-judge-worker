using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Application.UseCases.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand , string>
{
    private readonly IAuthRepository _repo;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public ForgotPasswordCommandHandler(
        IAuthRepository repo ,
        IEmailService email ,
        IConfiguration config)
    {
        _repo = repo;
        _email = email;
        _config = config;
    }

    public async Task<string> Handle(ForgotPasswordCommand request , CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _repo.FindByEmailAsync(email , ct);

        // Không leak email tồn tại hay không.
        if ( user == null )
            return string.Empty;

        var old = await _repo.GetVerificationsForUserAsync(user.UserId , ct);
        _repo.RemoveVerifications(old);

        var verification = new EmailVerification
        {
            UserId = user.UserId ,
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) ,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _repo.AddVerification(verification);

        await _repo.SaveAsync(ct);

        var frontendBaseUrl = _config["EmailSettings:FrontendBaseUrl"]?.TrimEnd('/');

        if ( string.IsNullOrWhiteSpace(frontendBaseUrl) )
            throw new InvalidOperationException("EmailSettings:FrontendBaseUrl is required.");

        var resetLink =
            $"{frontendBaseUrl}/reset-password" +
            $"?email={Uri.EscapeDataString(user.Email)}" +
            $"&token={Uri.EscapeDataString(verification.Token)}";

        var template =
            _config.GetSection("EmailSettings")["ForgotPasswordEmailTemplate"]
            ?? "<a href='{LINK}'>Khôi phục mật khẩu</a>";

        var body = template
            .Replace("{LINK}" , resetLink)
            .Replace("{YEAR}" , DateTime.UtcNow.Year.ToString());

        await _email.SendEmailAsync(
            user.Email ,
            "Khôi phục mật khẩu - TMOJ" ,
            body ,
            ct);

        return verification.Token;
    }
}