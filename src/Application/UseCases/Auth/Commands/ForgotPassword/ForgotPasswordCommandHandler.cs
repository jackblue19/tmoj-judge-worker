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

    public ForgotPasswordCommandHandler(IAuthRepository repo , IEmailService email , IConfiguration config)
    {
        _repo = repo;
        _email = email;
        _config = config;
    }

    public async Task<string> Handle(ForgotPasswordCommand request , CancellationToken ct)
    {
        var user = await _repo.FindByEmailAsync(request.Email.ToLowerInvariant() , ct);
        if ( user == null ) return string.Empty;

        var v = new EmailVerification
        {
            UserId = user.UserId ,
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) ,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
        _repo.AddVerification(v);
        await _repo.SaveAsync(ct);

        //var resetLink = $"http://api.tmoj.id.vn/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(v.Token)}";
        var frontendBaseUrl = _config["EmailSettings:FrontendBaseUrl"]?.TrimEnd('/')
                            ?? throw new InvalidOperationException("EmailSettings:FrontendBaseUrl is required.");

        var resetLink =
            $"{frontendBaseUrl}/reset-password" +
            $"?email={Uri.EscapeDataString(user.Email)}" +
            $"&token={Uri.EscapeDataString(v.Token)}";

        var template = _config.GetSection("EmailSettings")["ForgotPasswordEmailTemplate"]
                       ?? "<a href='{LINK}'>Khôi phục mật khẩu</a>";
        var body = template.Replace("{LINK}" , resetLink).Replace("{YEAR}" , DateTime.Now.Year.ToString());
        await _email.SendEmailAsync(user.Email , "Khôi phục mật khẩu - TMOJ" , body , ct);

        return v.Token;
    }
}
