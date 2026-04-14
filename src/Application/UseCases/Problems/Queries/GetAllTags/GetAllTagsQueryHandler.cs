using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetAllTags;

public sealed class GetAllTagsQueryHandler : IRequestHandler<GetAllTagsQuery , IReadOnlyList<AllTagsListItemDto>>
{
    private readonly IReadRepository<Tag , Guid> _tagReadRepository;

    public GetAllTagsQueryHandler(IReadRepository<Tag , Guid> tagReadRepository)
    {
        _tagReadRepository = tagReadRepository;
    }

    public async Task<IReadOnlyList<AllTagsListItemDto>> Handle(GetAllTagsQuery request , CancellationToken ct)
    {
        return await _tagReadRepository.ListAsync(new AllTagsSpec(request.IncludeInactive) , ct);
    }
}