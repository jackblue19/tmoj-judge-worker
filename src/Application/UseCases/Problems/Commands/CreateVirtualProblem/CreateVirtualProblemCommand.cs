using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Commands.CreateVirtualProblem;

public sealed record CreateVirtualProblemCommand(
    Guid? OriginProblemId ,
    string? OriginProblemSlug ,
    string? Slug ,
    string? Title ,
    string? VisibilityCode
) : IRequest<ProblemDetailDto>;