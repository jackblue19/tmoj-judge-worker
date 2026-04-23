using Application.UseCases.ProblemTemplates.Dtos;
using Ardalis.Specification;
using Domain.Constants;
using Domain.Entities;

namespace Application.UseCases.ProblemTemplates.Specifications;

public sealed class PublicProblemTemplatesByProblemIdSpec
    : Specification<ProblemTemplate , ProblemTemplateDto>
{
    public PublicProblemTemplatesByProblemIdSpec(Guid problemId)
    {
        Query
            .Where(x =>
                x.ProblemId == problemId &&
                x.IsActive &&
                x.Problem.IsActive &&
                x.Problem.StatusCode == ProblemStatusCodes.Published &&
                x.Problem.VisibilityCode == ProblemVisibilityCodes.Public)

            .OrderBy(x => x.RuntimeId)
            .ThenByDescending(x => x.Version);

        Query.Select(x => new ProblemTemplateDto
        {
            CodeTemplateId = x.CodeTemplateId ,
            ProblemId = x.ProblemId ,
            RuntimeId = x.RuntimeId ,
            TemplateCode = x.TemplateCode ,
            InjectionPoint = x.InjectionPoint ,
            SolutionSignature = x.SolutionSignature ,
            WrapperType = x.WrapperType ,
            Version = x.Version ,
            IsActive = x.IsActive ,
            CreatedAt = x.CreatedAt ,
            CreatedBy = x.CreatedBy
        })

        .AsNoTracking();
    }
}