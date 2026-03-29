namespace Application.UseCases.Testsets.Dtos;

public sealed class TestcaseContentItemDto
{
    public Guid TestcaseId { get; init; }
    public int Ordinal { get; init; }
    public bool IsSample { get; init; }
    public int Weight { get; init; }
    public string? Input { get; init; }
    public string? Output { get; init; }
}