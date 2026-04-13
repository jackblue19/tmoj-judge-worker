using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class TagByNameOrSlugSpec : Specification<Tag>
{
    public TagByNameOrSlugSpec(string name , string slug)
    {
        Query.Where(x =>
            x.Name.ToLower() == name.ToLower() ||
            x.Slug.ToLower() == slug.ToLower());
    }
}