using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateStudentStatus;

public record UpdateStudentStatusCommand(
    Guid ClassSemesterId,
    Guid StudentId,
    bool IsActive
) : IRequest;
