using Application.Abstractions.Outbound.Services;
using Application.Common.Events;
using Application.Common.Interfaces;
using Application.UseCases.Auth.Commands.ConfirmEmail;
using Application.UseCases.Auth.Dtos;
using Application.UseCases.Auth.Hasher;
using Application.UseCases.Auth.Options;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Application.UseCases.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;
    private readonly GoogleOptions _google;

    public RegisterCommandHandler(
        IAuthRepository repo,
        IPasswordHasher hasher,
        IEmailService email,
        IMediator mediator,
        IConfiguration config,
        IOptions<GoogleOptions> google)
    {
        _repo = repo;
        _hasher = hasher;
        _email = email;
        _mediator = mediator;
        _config = config;
        _google = google.Value;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.ToLowerInvariant();

        if (_google.AllowedDomains.Any() &&
            !_google.AllowedDomains.Any(d => email.EndsWith($"@{d}", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Registration with this email domain is not allowed.");

        var template = _config.GetSection("EmailSettings")["VerificationEmailTemplate"]
                       ?? "<a href='{LINK}'>Xác nhận Email</a>";
        string MakeHtml(string link) =>
            template.Replace("{LINK}", link).Replace("{YEAR}", DateTime.Now.Year.ToString());

        var roleCode = email.EndsWith("@fe.edu.vn") ? "teacher" : "student";
        var role = await _repo.FindRoleAsync(roleCode, ct) ?? throw new Exception("Role not found");

        var existing = await _repo.FindByEmailAsync(email, ct);
        string verificationToken;

        if (existing != null)
        {
            if (existing.EmailVerified)
                throw new InvalidOperationException("Email already exists and verified");

            existing.FirstName = request.FirstName;
            existing.LastName = request.LastName;
            existing.DisplayName = $"{request.LastName} {request.FirstName}";
            existing.AvatarUrl = request.Avatar;
            existing.Password = _hasher.Hash(request.Password);
            existing.LanguagePreference = "vi";
            existing.EmailVerified = false;
            if (existing.RoleId == null) existing.RoleId = role.RoleId;

            var old = await _repo.GetVerificationsForUserAsync(existing.UserId, ct);
            _repo.RemoveVerifications(old);

            var v = new EmailVerification
            {
                UserId = existing.UserId,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
            _repo.AddVerification(v);
            await _repo.SaveAsync(ct);
            verificationToken = v.Token;
        }
        else
        {
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = email,
                Password = _hasher.Hash(request.Password),
                Username = email.Split('@')[0] + Random.Shared.Next(1000, 9999).ToString(),
                DisplayName = $"{request.LastName} {request.FirstName}",
                AvatarUrl = request.Avatar,
                LanguagePreference = "vi",
                Status = true,
                EmailVerified = false,
                RoleId = role.RoleId
            };

            var v = new EmailVerification
            {
                User = user,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _repo.Add(user);
            _repo.AddVerification(v);
            await _repo.SaveAsync(ct);
            verificationToken = v.Token;
        }

        var confirmLink = $"https://api.tmoj.id.vn/api/v1/Auth/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(verificationToken)}";
        await _email.SendEmailAsync(email, "Xác nhận địa chỉ email - TMOJ", MakeHtml(confirmLink), ct);

        return await _mediator.Send(new ConfirmEmailCommand(email, verificationToken, request.IpAddress, request.UserAgent), ct);
    }
}
