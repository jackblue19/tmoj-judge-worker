using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed record SetProblemDifficultyCommand(
    Guid ProblemId,
    string Difficulty
) : IRequest<ProblemDetailDto>;
