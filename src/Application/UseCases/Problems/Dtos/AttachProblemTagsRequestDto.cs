namespace Application.UseCases.Problems.Dtos;

public sealed class AttachProblemTagsRequestDto
{
    public IReadOnlyCollection<Guid> TagIds { get; init; } = [];
}
