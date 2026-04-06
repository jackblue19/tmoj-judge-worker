using MediatR;
using Application.Common.Pagination;

namespace Application.UseCases.Editorials;

public record ViewEditorialQuery(
    Guid ProblemId,
    Guid? CursorId,
    DateTime? CursorCreatedAt,
    int PageSize = 10
) : IRequest<CursorPaginationDto<EditorialDto>>;
