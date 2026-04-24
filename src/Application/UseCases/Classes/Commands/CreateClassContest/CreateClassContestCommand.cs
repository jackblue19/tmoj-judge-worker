using MediatR;

namespace Application.UseCases.Classes.Commands.CreateClassContest;

public record CreateClassContestCommand(
    Guid ClassSemesterId,
    Guid CreatedByUserId,
    string Title,
    string? Slug,
    string? DescriptionMd,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? FreezeAt,
    string? Rules,
    List<ContestProblemItem>? Problems,
    int? SlotNo,
    string? SlotTitle
) : IRequest<(Guid ContestId, Guid SlotId)>;

public record ContestProblemItem(
    Guid ProblemId,
    int? Ordinal,
    string? Alias,
    int? Points,
    int? MaxScore,
    int? TimeLimitMs,
    int? MemoryLimitKb);
