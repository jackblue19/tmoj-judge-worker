using Application.UseCases.Auth.Dtos;
using MediatR;

namespace Application.UseCases.Auth.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Avatar,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthResponseDto>;
