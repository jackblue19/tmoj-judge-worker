using MediatR;
using Application.Common.Pagination;
using Application.UseCases.Editorials.Dtos;

namespace Application.UseCases.Editorials.Queries;

public record ViewEditorialQuery(
    Guid ProblemId,
    Guid? CursorId,
    DateTime? CursorCreatedAt,
    int PageSize = 10
) : IRequest<CursorPaginationDto<EditorialDto>>;
