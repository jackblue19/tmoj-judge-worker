using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetAvailableStudents;

public record GetAvailableStudentsQuery(
    Guid ClassSemesterId,
    string? Search,
    int PageNumber,
    int PageSize
) : IRequest<PagedAvailableStudentsDto>;
