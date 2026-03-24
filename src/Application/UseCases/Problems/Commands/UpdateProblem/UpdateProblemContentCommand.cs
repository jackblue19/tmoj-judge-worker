using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed record UpdateProblemContentCommand(
    Guid ProblemId,
    string Title,
    string Slug,
    string? DescriptionMd,
    int? TimeLimitMs,
    int? MemoryLimitKb,
    string? TypeCode,
    string? ScoringCode,
    string? VisibilityCode
) : IRequest<ProblemDetailDto>;
