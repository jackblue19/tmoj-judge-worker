using Application.Common.Events;
using Application.Common.Interfaces;
using Application.UseCases.Auth.Commands.ConfirmEmail;
using Application.UseCases.Auth.Dtos;
using Application.UseCases.Auth.Hasher;
using MediatR;

namespace Application.UseCases.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ConfirmEmailCommandHandler _authBuilder;
    private readonly IMediator _mediator;

    public LoginCommandHandler(
        IAuthRepository repo,
        IPasswordHasher hasher,
        ConfirmEmailCommandHandler authBuilder,
        IMediator mediator)
    {
        _repo = repo;
        _hasher = hasher;
        _authBuilder = authBuilder;
        _mediator = mediator;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.ToLowerInvariant();
        var user = await _repo.FindByEmailWithRoleAsync(email, ct);

        if (user == null || string.IsNullOrEmpty(user.Password) || !_hasher.Verify(request.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.EmailVerified)
            throw new InvalidOperationException("Please verify your email before logging in.");

        if (!user.Status)
            throw new InvalidOperationException("Your account has been locked.");

        var response = await _authBuilder.BuildAuthResponseAsync(user, request.IpAddress, request.UserAgent, ct);
        await _mediator.Publish(new DailyLoginEvent(user.UserId), ct);

        return response;
    }
}
