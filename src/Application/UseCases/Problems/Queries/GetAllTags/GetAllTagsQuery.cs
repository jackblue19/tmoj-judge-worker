using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetAllTags;

public sealed record GetAllTagsQuery(
    bool IncludeInactive = false
) : IRequest<IReadOnlyList<AllTagsListItemDto>>;