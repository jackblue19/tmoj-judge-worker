using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassStudents;

public record GetClassStudentsQuery(Guid ClassSemesterId) : IRequest<List<ClassMemberDto>>;
