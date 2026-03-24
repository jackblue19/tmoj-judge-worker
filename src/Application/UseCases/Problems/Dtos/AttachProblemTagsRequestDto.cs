using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Dtos;

public sealed class AttachProblemTagsRequestDto
{
    public IReadOnlyCollection<Guid> TagIds { get; init; } = [];
}
