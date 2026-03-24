using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed record CreateProblemDraftCommand(
    string Title,
    string Slug,
    int? TimeLimitMs,
    int? MemoryLimitKb,
    string? TypeCode,
    string? ScoringCode,
    string? VisibilityCode,
    string? DescriptionMd
) : IRequest<ProblemSummaryDto>;