using Application.Common.Pagination;
using MediatR;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByUser;

public sealed record GetSubmissionsByUserQuery(
    Guid UserId ,
    Guid CurrentUserId ,
    bool IsElevated ,
    int Page = 1 ,
    int PageSize = 20
) : IRequest<ApiPagedResponse<SubmissionByUserListItemDto>>;