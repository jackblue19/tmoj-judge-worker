using MediatR;

namespace Application.UseCases.Classes.Commands.RemoveStudent;

public record RemoveStudentCommand(
    Guid ClassSemesterId,
    Guid StudentId
) : IRequest;
