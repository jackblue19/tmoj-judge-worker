namespace Application.UseCases.Classes.Dtos;

public sealed class AddClassContestProblemRequestDto
{
    public Guid ProblemId { get; init; }
    public string? Alias { get; init; }
    public int? Ordinal { get; init; }
    public int? Points { get; init; }
    public int? MaxScore { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
}
