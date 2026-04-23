using Application.UseCases.ProblemTemplates.Dtos;
using MediatR;

namespace Application.UseCases.ProblemTemplates.Queries.GetProblemTemplatesByProblemId;

public sealed record GetProblemTemplatesByProblemIdQuery(Guid ProblemId)
    : IRequest<IReadOnlyList<ProblemTemplateDto>>;