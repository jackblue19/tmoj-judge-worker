using Application.UseCases.Problems.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed record CreateProblemDraftCommand(
    string Title,
    string Slug,
    int? TimeLimitMs,
    int? MemoryLimitKb,
    string? TypeCode,
    string? ScoringCode,
    string? VisibilityCode,
    string? DescriptionMd,
    IFormFile? StatementFile
) : IRequest<ProblemSummaryDto>;