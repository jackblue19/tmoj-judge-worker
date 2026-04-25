using Application.UseCases.Auth.Dtos;
using MediatR;

namespace Application.UseCases.Auth.Commands.SocialLogin;

public record SocialLoginCommand(
    string Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? AvatarUrl,
    string ProviderCode,
    string ProviderSubject,
    bool EmailVerified,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthResponseDto>;
