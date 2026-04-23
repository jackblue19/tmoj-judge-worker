using Application.UseCases.ProblemTemplates.Dtos;
using MediatR;

namespace Application.UseCases.ProblemTemplates.Commands.DeleteProblemTemplate;

public sealed record DeleteProblemTemplateCommand(Guid CodeTemplateId)
    : IRequest<ProblemTemplateDto>;