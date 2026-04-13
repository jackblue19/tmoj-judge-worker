using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Commands.ReplaceProblemTags;

public sealed record ReplaceProblemTagsCommand(
    Guid ProblemId ,
    IReadOnlyCollection<Guid> TagIds
) : IRequest<ProblemDetailDto>;