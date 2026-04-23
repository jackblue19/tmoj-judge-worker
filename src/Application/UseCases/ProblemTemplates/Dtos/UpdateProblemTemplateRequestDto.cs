namespace Application.UseCases.ProblemTemplates.Dtos;

public sealed class UpdateProblemTemplateRequestDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string? InjectionPoint { get; set; }
    public string? SolutionSignature { get; set; }
    public bool? IsActive { get; set; }
}