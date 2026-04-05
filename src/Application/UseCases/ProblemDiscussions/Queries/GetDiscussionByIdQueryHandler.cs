using Application.Common.Interfaces;
using Application.UseCases.ProblemDiscussions.Dtos;
using MediatR;

namespace Application.UseCases.ProblemDiscussions.Queries;

public class GetDiscussionByIdQueryHandler
    : IRequestHandler<GetDiscussionByIdQuery, DiscussionResponseDto>
{
    private readonly IProblemDiscussionRepository _repo;

    public GetDiscussionByIdQueryHandler(IProblemDiscussionRepository repo)
    {
        _repo = repo;
    }

    public async Task<DiscussionResponseDto> Handle(
        GetDiscussionByIdQuery request,
        CancellationToken ct)
    {
        var discussion = await _repo.GetByIdAsync(request.Id);

        if (discussion is null)
            throw new Exception("Discussion not found");

        return discussion;
    }
}