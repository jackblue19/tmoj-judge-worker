using Application.UseCases.Auth.Dtos;
using MediatR;

namespace Application.UseCases.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(
    string Email,
    string Token,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthResponseDto>;
