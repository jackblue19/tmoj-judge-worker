using Application.UseCases.Auth.Dtos;
using MediatR;

namespace Application.UseCases.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthResponseDto>;
