using Application.Common.Pagination;
using Application.UseCases.Editorials.Dtos;
using Application.UseCases.Editorials.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Editorials.Queries;

public class ViewEditorialQueryHandler
    : IRequestHandler<ViewEditorialQuery, CursorPaginationDto<EditorialDto>>
{
    private readonly IReadRepository<Editorial, Guid> _repo;

    public ViewEditorialQueryHandler(IReadRepository<Editorial, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<CursorPaginationDto<EditorialDto>> Handle(ViewEditorialQuery request, CancellationToken ct)
    {
        // Fetch pageSize + 1 to determine hasMore
        var spec = new ViewEditorialSpec(
            request.ProblemId,
            request.CursorId,
            request.CursorCreatedAt,
            request.PageSize + 1
        );

        var items = await _repo.ListAsync(spec, ct);

        var hasMore = items.Count > request.PageSize;
        var resultItems = hasMore
            ? items.Take(request.PageSize).ToList()
            : items.ToList();

        var result = new CursorPaginationDto<EditorialDto>
        {
            Items = resultItems,
            HasMore = hasMore
        };

        if (resultItems.Count > 0)
        {
            var last = resultItems[^1];
            result.NextCursorCreatedAt = last.CreatedAt;
            result.NextCursorId = last.EditorialId;
        }

        return result;
    }
}