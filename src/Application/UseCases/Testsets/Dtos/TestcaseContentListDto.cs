namespace Application.UseCases.Testsets.Dtos;

public sealed class TestcaseContentListDto
{
    public Guid ProblemId { get; init; }
    public Guid TestsetId { get; init; }
    public int Count { get; init; }
    public IReadOnlyList<TestcaseContentItemDto> Items { get; init; } = [];
}