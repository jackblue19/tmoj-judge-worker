using Application.UseCases.Problems.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed record CreateProblemCommand(
    string Title ,
    string Slug ,
    string? Difficulty ,
    string? TypeCode ,
    string? VisibilityCode ,
    string? ScoringCode ,
    string? StatusCode ,
    int? TimeLimitMs ,
    int? MemoryLimitKb ,
    string? DescriptionMd ,
    IFormFile? StatementFile ,
    IReadOnlyCollection<Guid>? TagIds
) : IRequest<ProblemDetailDto>;