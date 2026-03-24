using Application.UseCases.Problems.Dtos;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetProblemById;

public sealed record GetProblemDetailQuery(Guid ProblemId) : IRequest<ProblemDetailDto>;
