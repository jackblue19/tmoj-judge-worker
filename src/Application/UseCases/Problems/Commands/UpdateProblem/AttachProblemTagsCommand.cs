using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed record AttachProblemTagsCommand(
    Guid ProblemId,
    IReadOnlyCollection<Guid> TagIds
) : IRequest<ProblemDetailDto>;
