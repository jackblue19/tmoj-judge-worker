using Application.UseCases.ProblemTemplates.Dtos;
using Domain.Entities;

namespace Application.UseCases.ProblemTemplates.Mappings;

public static class ProblemTemplateMappings
{
    public static ProblemTemplateDto ToDto(this ProblemTemplate entity)
    {
        return new ProblemTemplateDto
        {
            CodeTemplateId = entity.CodeTemplateId ,
            ProblemId = entity.ProblemId ,
            RuntimeId = entity.RuntimeId ,
            TemplateCode = entity.TemplateCode ,
            InjectionPoint = entity.InjectionPoint ,
            SolutionSignature = entity.SolutionSignature ,
            WrapperType = entity.WrapperType ,
            Version = entity.Version ,
            IsActive = entity.IsActive ,
            CreatedAt = entity.CreatedAt ,
            CreatedBy = entity.CreatedBy
        };
    }
}