using MediatR;

namespace Application.UseCases.Classes.Commands.RemoveClassSemester;

public record RemoveClassSemesterCommand(Guid ClassId, Guid ClassSemesterId) : IRequest;
