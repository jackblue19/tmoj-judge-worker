using Application.UseCases.ProblemTemplates.Dtos;
using MediatR;

namespace Application.UseCases.ProblemTemplates.Commands.UpdateProblemTemplate;

public sealed record UpdateProblemTemplateCommand(
    Guid CodeTemplateId ,
    string TemplateCode ,
    string? InjectionPoint ,
    string? SolutionSignature ,
    bool? IsActive
) : IRequest<ProblemTemplateDto>;