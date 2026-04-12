using Application.Common.Pagination;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetProblemsByUser;

public sealed record GetProblemsByUserQuery(
    Guid UserId ,
    Guid CurrentUserId ,
    bool IsElevated ,
    int Page = 1 ,
    int PageSize = 20
) : IRequest<ApiPagedResponse<ProblemByUserListItemDto>>;