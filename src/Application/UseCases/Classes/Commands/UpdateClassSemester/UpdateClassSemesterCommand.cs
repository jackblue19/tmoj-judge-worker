using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateClassSemester;

public record UpdateClassSemesterCommand(
    Guid ClassId,
    Guid ClassSemesterId,
    Guid? NewClassId,
    Guid? SemesterId,
    Guid? SubjectId,
    Guid? TeacherId
) : IRequest;
