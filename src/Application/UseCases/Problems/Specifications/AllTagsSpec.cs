using Ardalis.Specification;
using Application.UseCases.Problems.Dtos;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class AllTagsSpec : Specification<Tag , AllTagsListItemDto>
{
    public AllTagsSpec(bool includeInactive = false)
    {
        if ( !includeInactive )
            Query.Where(x => x.IsActive);

        Query.OrderBy(x => x.Name);

        Query.Select(x => new AllTagsListItemDto
        {
            Id = x.Id ,
            Name = x.Name ,
            Slug = x.Slug ,
            Description = x.Description ,
            Color = x.Color ,
            Icon = x.Icon ,
            IsActive = x.IsActive ,
            CreatedAt = x.CreatedAt
        });
    }
}