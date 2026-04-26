using Application.Common.Interfaces;
using Application.UseCases.Auth.Commands.ConfirmEmail;
using Application.UseCases.Auth.Dtos;
using Application.UseCases.Auth.Service;
using MediatR;

namespace Application.UseCases.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IAuthRepository _repo;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ConfirmEmailCommandHandler _authBuilder;

    public RefreshTokenCommandHandler(
        IAuthRepository repo,
        IRefreshTokenService refreshTokenService,
        ConfirmEmailCommandHandler authBuilder)
    {
        _repo = repo;
        _refreshTokenService = refreshTokenService;
        _authBuilder = authBuilder;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = _refreshTokenService.HashToken(request.RefreshToken);
        var token = await _repo.FindActiveRefreshTokenAsync(hash, ct)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (token.ExpireAt < DateTime.UtcNow || token.RevokedAt != null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (!token.Session.User.Status)
            throw new UnauthorizedAccessException("Your account has been locked.");

        token.RevokedAt = DateTime.UtcNow;
        await _repo.SaveAsync(ct);

        return await _authBuilder.BuildAuthResponseAsync(token.Session.User, request.IpAddress, request.UserAgent, ct);
    }
}
