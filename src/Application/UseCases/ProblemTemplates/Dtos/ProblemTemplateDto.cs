namespace Application.UseCases.ProblemTemplates.Dtos;

public sealed class ProblemTemplateDto
{
    public Guid CodeTemplateId { get; init; }
    public Guid ProblemId { get; init; }
    public Guid RuntimeId { get; init; }

    public string TemplateCode { get; init; } = string.Empty;
    public string InjectionPoint { get; init; } = "{{USER_CODE}}";
    public string? SolutionSignature { get; init; }
    public string WrapperType { get; init; } = "full";

    public int Version { get; init; }
    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
}