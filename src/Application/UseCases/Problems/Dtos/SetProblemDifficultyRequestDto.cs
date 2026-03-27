namespace Application.UseCases.Problems.Dtos;

public sealed class SetProblemDifficultyRequestDto
{
    public string Difficulty { get; init; } = string.Empty;
}