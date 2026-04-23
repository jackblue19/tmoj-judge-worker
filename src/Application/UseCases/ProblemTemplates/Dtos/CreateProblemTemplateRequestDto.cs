namespace Application.UseCases.ProblemTemplates.Dtos;

public sealed class CreateProblemTemplateRequestDto
{
    public Guid RuntimeId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string? InjectionPoint { get; set; }
    public string? SolutionSignature { get; set; }
    public int? Version { get; set; }
}