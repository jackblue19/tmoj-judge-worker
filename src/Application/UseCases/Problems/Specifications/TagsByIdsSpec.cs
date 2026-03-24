using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Problems.Specifications;

public sealed class TagsByIdsSpec : Specification<Tag>
{
    public TagsByIdsSpec(IEnumerable<Guid> tagIds)
    {
        var ids = tagIds.Distinct().ToArray();
        Query.Where(x => ids.Contains(x.Id));
    }
}
