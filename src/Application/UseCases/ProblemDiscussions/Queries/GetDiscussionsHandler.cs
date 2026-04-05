using MediatR;
using Application.Common.Interfaces;
using Application.Common.Pagination;
using Application.UseCases.ProblemDiscussions.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using Application.UseCases.Editorials;

namespace Application.UseCases.ProblemDiscussions.Queries;

public class GetDiscussionsHandler
    : IRequestHandler<GetDiscussionsQuery, CursorPaginationDto<DiscussionResponseDto>>
{
    private readonly IProblemDiscussionRepository _repo;
    private readonly IReadRepository<Problem, Guid> _problemRepo;

    public GetDiscussionsHandler(
        IProblemDiscussionRepository repo,
        IReadRepository<Problem, Guid> problemRepo)
    {
        _repo = repo;
        _problemRepo = problemRepo;
    }

    public async Task<CursorPaginationDto<DiscussionResponseDto>> Handle(
        GetDiscussionsQuery query,
        CancellationToken ct)
    {
        // Validate problem exists (moved from controller)
        var problem = await _problemRepo.GetByIdAsync(query.ProblemId, ct);
        if (problem is null || !problem.IsActive)
            throw new Exception("Problem not found.");

        return await _repo.GetPagedAsync(
            query.ProblemId,
            query.CursorCreatedAt,
            query.CursorId,
            query.PageSize
        );
    }
}
