using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetMyClasses;

public record GetMyClassesQuery(
    Guid UserId,
    string Role,
    Guid? SemesterId,
    Guid? SubjectId,
    int PageNumber,
    int PageSize) : IRequest<ClassListDto>;
