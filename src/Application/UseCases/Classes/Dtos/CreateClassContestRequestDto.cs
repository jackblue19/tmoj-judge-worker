using Application.UseCases.Classes.Commands.CreateClassContest;

namespace Application.UseCases.Classes.Dtos;

public sealed class CreateClassContestRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? DescriptionMd { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public DateTime? FreezeAt { get; init; }
    public string? Rules { get; init; }
    public List<ContestProblemItem>? Problems { get; init; }
    public int? SlotNo { get; init; }
    public string? SlotTitle { get; init; }
}
