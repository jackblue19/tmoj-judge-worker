namespace Application.UseCases.Problems.Dtos;

public sealed class ReplaceProblemTagsRequestDto
{
    public IReadOnlyCollection<Guid> TagIds { get; init; } = [];
}
