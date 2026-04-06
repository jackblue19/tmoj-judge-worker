using MediatR;
using Application.Common.Pagination;
using Application.UseCases.ProblemDiscussions.Dtos;
using Application.UseCases.Editorials;

namespace Application.UseCases.ProblemDiscussions.Queries;

public record GetDiscussionsQuery(
    Guid ProblemId,
    DateTime? CursorCreatedAt,
    Guid? CursorId,
    int PageSize
) : IRequest<CursorPaginationDto<DiscussionResponseDto>>;
