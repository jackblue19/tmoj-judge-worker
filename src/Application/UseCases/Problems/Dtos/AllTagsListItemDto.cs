namespace Application.UseCases.Problems.Dtos;

public sealed class AllTagsListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}