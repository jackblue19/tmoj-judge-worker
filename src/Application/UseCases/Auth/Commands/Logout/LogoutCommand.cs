using MediatR;

namespace Application.UseCases.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId) : IRequest;
