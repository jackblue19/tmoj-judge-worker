using MediatR;

namespace Application.UseCases.Classes.Commands.AssignTeacherRole;

public record AssignTeacherRoleCommand(Guid UserId) : IRequest;
