namespace Application.UseCases.Problems.Dtos;

public sealed class ProblemTagDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public bool IsActive { get; init; }
}
