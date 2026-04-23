using Application.UseCases.Problems.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed record UpdateProblemContentCommand(
    Guid ProblemId ,
    string Title ,
    string Slug ,
    string? Difficulty ,
    string? TypeCode ,
    string? ScoringCode ,
    int? TimeLimitMs ,
    int? MemoryLimitKb ,
    string? DescriptionMd ,
    IFormFile? StatementFile ,
    IReadOnlyCollection<Guid>? TagIds ,
    string? ProblemMode
) : IRequest<ProblemDetailDto>;