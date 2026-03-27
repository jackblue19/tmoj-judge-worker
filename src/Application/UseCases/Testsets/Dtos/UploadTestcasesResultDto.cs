using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Testsets.Dtos;

public sealed class UploadTestcasesResultDto
{
    public Guid ProblemId { get; init; }
    public string Slug { get; init; } = null!;
    public Guid TestsetId { get; init; }
    public int Total { get; init; }
    public IReadOnlyList<TestcaseUploadedItemDto> Items { get; init; } = [];
}

public sealed class TestcaseUploadedItemDto
{
    public int Ordinal { get; init; }
    public string InputObjectKey { get; init; } = null!;
    public string OutputObjectKey { get; init; } = null!;
}
