using Application.UseCases.Problems.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Commands.CreateRemixProblem;

public sealed record CreateRemixProblemCommand(
    Guid? OriginProblemId ,
    string? OriginProblemSlug ,
    string? Title ,
    string? Slug ,
    string? Difficulty ,
    string? TypeCode ,
    string? VisibilityCode ,
    string? ScoringCode ,
    int? TimeLimitMs ,
    int? MemoryLimitKb ,
    string? DescriptionMd ,
    IFormFile? StatementFile ,
    IReadOnlyCollection<Guid>? TagIds ,
    string? ProblemMode
) : IRequest<ProblemDetailDto>;