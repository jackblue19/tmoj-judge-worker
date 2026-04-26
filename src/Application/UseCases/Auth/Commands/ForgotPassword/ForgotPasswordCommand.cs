using MediatR;

namespace Application.UseCases.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<string>;
