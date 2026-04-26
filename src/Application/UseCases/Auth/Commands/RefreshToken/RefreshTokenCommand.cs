using Application.UseCases.Auth.Dtos;
using MediatR;

namespace Application.UseCases.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthResponseDto>;
