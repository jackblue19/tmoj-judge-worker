using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Testsets.Dtos;

public sealed class SampleTestcaseItemDto
{
    public int Ordinal { get; init; }
    public string Input { get; init; } = null!;
    public string Output { get; init; } = null!;
}

public sealed class SampleTestcaseListDto
{
    public Guid ProblemId { get; init; }
    public Guid TestsetId { get; init; }
    public int Count { get; init; }
    public IReadOnlyList<SampleTestcaseItemDto> Items { get; init; } = [];
}
