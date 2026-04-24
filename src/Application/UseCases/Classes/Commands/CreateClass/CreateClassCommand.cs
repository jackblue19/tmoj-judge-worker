using MediatR;

namespace Application.UseCases.Classes.Commands.CreateClass;

public record CreateClassCommand(
    string Code,
    Guid SubjectId,
    Guid SemesterId,
    Guid? TeacherId
) : IRequest<(Guid ClassId, Guid InstanceId)>;
