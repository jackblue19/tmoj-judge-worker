using Application.UseCases.Semesters.Dtos;
using MediatR;

namespace Application.UseCases.Semesters.Queries.GetAllSemesters;

public record GetAllSemestersQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<SemesterListDto>;
