using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Commands.CreateTag;

public sealed record CreateTagCommand(
    string Name ,
    string? Slug ,
    string? Description ,
    string? Color ,
    string? Icon
) : IRequest<ProblemTagDto>;