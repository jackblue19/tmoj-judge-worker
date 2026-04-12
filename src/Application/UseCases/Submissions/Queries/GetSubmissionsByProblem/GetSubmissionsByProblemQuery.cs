using Application.Common.Pagination;
using MediatR;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByProblem;

public sealed record GetSubmissionsByProblemQuery(
    Guid ProblemId ,
    Guid CurrentUserId ,
    bool IsElevated ,
    int Page = 1 ,
    int PageSize = 20
) : IRequest<ApiPagedResponse<SubmissionListItemDto>>;