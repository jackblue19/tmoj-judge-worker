namespace Application.UseCases.Problems.Dtos;

public sealed class CreateTagRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
}
