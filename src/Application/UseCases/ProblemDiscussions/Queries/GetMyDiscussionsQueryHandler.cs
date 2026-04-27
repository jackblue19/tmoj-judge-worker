using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.ProblemDiscussions.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.ProblemDiscussions.Queries;

public class GetMyDiscussionsQueryHandler : IRequestHandler<GetMyDiscussionsQuery, CursorPaginationDto<DiscussionResponseDto>>
{
    private readonly IProblemDiscussionRepository _repository;

    public GetMyDiscussionsQueryHandler(IProblemDiscussionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CursorPaginationDto<DiscussionResponseDto>> Handle(GetMyDiscussionsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetPagedByUserAsync(
            request.UserId,
            request.CursorCreatedAt,
            request.CursorId,
            request.PageSize);
    }
}
