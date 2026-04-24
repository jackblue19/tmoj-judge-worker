using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClasses;

public record GetClassesQuery(
    Guid? SemesterId,
    Guid? SubjectId,
    string? Search,
    int PageNumber,
    int PageSize) : IRequest<ClassListDto>;
