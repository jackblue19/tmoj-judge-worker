using Application.UseCases.ProblemTemplates.Dtos;
using MediatR;

namespace Application.UseCases.ProblemTemplates.Commands.CreateProblemTemplate;

public sealed record CreateProblemTemplateCommand(
    Guid ProblemId ,
    Guid RuntimeId ,
    string TemplateCode ,
    string? InjectionPoint ,
    string? SolutionSignature ,
    int? Version
) : IRequest<ProblemTemplateDto>;