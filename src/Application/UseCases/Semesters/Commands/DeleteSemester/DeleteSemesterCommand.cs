using MediatR;

namespace Application.UseCases.Semesters.Commands.DeleteSemester;

public record DeleteSemesterCommand(Guid SemesterId) : IRequest;
