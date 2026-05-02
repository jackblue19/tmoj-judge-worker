namespace Application.UseCases.Classes.Dtos;

public sealed class ClassContestSubmitRequestDto
{
    public Guid ContestProblemId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
}
