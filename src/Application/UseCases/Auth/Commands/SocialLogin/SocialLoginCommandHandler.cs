using Application.Common.Events;
using Application.Common.Interfaces;
using Application.UseCases.Auth.Commands.ConfirmEmail;
using Application.UseCases.Auth.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Auth.Commands.SocialLogin;

public class SocialLoginCommandHandler : IRequestHandler<SocialLoginCommand, AuthResponseDto>
{
    private readonly IAuthRepository _repo;
    private readonly ConfirmEmailCommandHandler _authBuilder;
    private readonly IMediator _mediator;

    public SocialLoginCommandHandler(
        IAuthRepository repo,
        ConfirmEmailCommandHandler authBuilder,
        IMediator mediator)
    {
        _repo = repo;
        _authBuilder = authBuilder;
        _mediator = mediator;
    }

    public async Task<AuthResponseDto> Handle(SocialLoginCommand request, CancellationToken ct)
    {
        var email = request.Email.ToLowerInvariant();
        var provider = await _repo.GetOrCreateProviderAsync(request.ProviderCode,
            char.ToUpper(request.ProviderCode[0]) + request.ProviderCode[1..], ct);

        var user = await _repo.FindByEmailWithRoleAndProvidersAsync(email, ct);

        if (user == null)
        {
            var studentRole = await _repo.FindRoleAsync("student", ct);
            user = new User
            {
                Email = email,
                FirstName = request.FirstName ?? "",
                LastName = request.LastName ?? "",
                DisplayName = request.DisplayName,
                AvatarUrl = request.AvatarUrl,
                Username = email.Split('@')[0] + Random.Shared.Next(1000, 9999).ToString(),
                EmailVerified = request.EmailVerified,
                LanguagePreference = "vi",
                Status = true,
                UserProviders = new List<UserProvider>()
            };
            user.UserProviders.Add(new UserProvider
            {
                ProviderId = provider.ProviderId,
                ProviderSubject = request.ProviderSubject,
                ProviderEmail = email
            });
            if (studentRole != null) user.RoleId = studentRole.RoleId;

            _repo.Add(user);
            await _repo.SaveAsync(ct);
        }
        else if (!user.UserProviders.Any(p => p.ProviderId == provider.ProviderId))
        {
            user.UserProviders.Add(new UserProvider
            {
                ProviderId = provider.ProviderId,
                ProviderSubject = request.ProviderSubject,
                ProviderEmail = email
            });
            if (string.IsNullOrEmpty(user.AvatarUrl)) user.AvatarUrl = request.AvatarUrl;
            if (string.IsNullOrEmpty(user.FirstName)) user.FirstName = request.FirstName ?? "";
            if (string.IsNullOrEmpty(user.LastName)) user.LastName = request.LastName ?? "";
            if (string.IsNullOrEmpty(user.DisplayName)) user.DisplayName = request.DisplayName ?? "";
            if (request.EmailVerified) user.EmailVerified = true;
            await _repo.SaveAsync(ct);
        }

        var response = await _authBuilder.BuildAuthResponseAsync(user, request.IpAddress, request.UserAgent, ct);
        await _mediator.Publish(new DailyLoginEvent(user.UserId), ct);
        return response;
    }
}
