using Application.Common.Pagination;
using Application.UseCases.ProblemDiscussions.Dtos;
using MediatR;
using System;

namespace Application.UseCases.ProblemDiscussions.Queries;

public record GetMyDiscussionsQuery(
    Guid UserId,
    DateTime? CursorCreatedAt = null,
    Guid? CursorId = null,
    int PageSize = 10) : IRequest<CursorPaginationDto<DiscussionResponseDto>>;
