using Application.Common.Interfaces;
using Application.UseCases.Auth.Dtos;
using Application.UseCases.Auth.Options;
using Application.UseCases.Auth.Service;
using Domain.Entities;
using DomainRefreshToken = Domain.Entities.RefreshToken;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.UseCases.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, AuthResponseDto>
{
    private readonly IAuthRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtOptions _jwt;

    public ConfirmEmailCommandHandler(
        IAuthRepository repo,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtOptions> jwt)
    {
        _repo = repo;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _jwt = jwt.Value;
    }

    public async Task<AuthResponseDto> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var email = request.Email.ToLowerInvariant();
        var verification = await _repo.FindVerificationWithUserAsync(email, request.Token, ct)
            ?? throw new KeyNotFoundException("Invalid or expired verification token.");

        if (verification.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired verification token.");

        verification.User.EmailVerified = true;
        _repo.RemoveVerification(verification);
        await _repo.SaveAsync(ct);

        return await BuildAuthResponseAsync(verification.User, request.IpAddress, request.UserAgent, ct);
    }

    internal async Task<AuthResponseDto> BuildAuthResponseAsync(User user, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var session = new UserSession
        {
            UserId = user.UserId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        var refreshTokenStr = _refreshTokenService.GenerateToken();
        session.RefreshTokens.Add(new DomainRefreshToken
        {
            TokenHash = _refreshTokenService.HashToken(refreshTokenStr),
            ExpireAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        _repo.AddSession(session);
        await _repo.SaveAsync(ct);

        var role = user.Role?.RoleCode ?? "user";
        var accessToken = _tokenService.CreateAccessToken(
            user.UserId.ToString(),
            user.DisplayName ?? user.Username,
            new[] { role });

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenStr,
            ExpiresIn: _jwt.AccessTokenMinutes * 60,
            User: new AuthUserDto(
                user.UserId, user.Email,
                user.FirstName, user.LastName,
                user.DisplayName, user.Username,
                user.AvatarUrl, user.EmailVerified, role));
    }
}
