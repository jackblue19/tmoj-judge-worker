using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Dtos;
using MediatR;

namespace Application.UseCases.DiscussionComments.Queries;

public class GetCommentsByDiscussionQueryHandler
    : IRequestHandler<GetCommentsByDiscussionQuery, List<CommentResponseDto>>
{
    private readonly IDiscussionCommentRepository _repo;

    public GetCommentsByDiscussionQueryHandler(
        IDiscussionCommentRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<CommentResponseDto>> Handle(
        GetCommentsByDiscussionQuery request,
        CancellationToken ct)
    {
        return await _repo.GetByDiscussionIdAsync(request.DiscussionId);
    }
}