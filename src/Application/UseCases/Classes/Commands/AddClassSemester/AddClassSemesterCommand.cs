using MediatR;

namespace Application.UseCases.Classes.Commands.AddClassSemester;

public record AddClassSemesterCommand(
    Guid ClassId,
    Guid SemesterId,
    Guid SubjectId,
    Guid? TeacherId
) : IRequest<Guid>;
