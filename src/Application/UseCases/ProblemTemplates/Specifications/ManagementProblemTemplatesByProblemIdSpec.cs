using Application.UseCases.ProblemTemplates.Dtos;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemTemplates.Specifications;

public sealed class ManagementProblemTemplatesByProblemIdSpec
    : Specification<ProblemTemplate , ProblemTemplateDto>
{
    public ManagementProblemTemplatesByProblemIdSpec(Guid problemId , Guid currentUserId , bool isAdmin)
    {
        Query
            .Where(x =>
                x.ProblemId == problemId &&
                x.Problem.IsActive &&
                (isAdmin || x.Problem.CreatedBy == currentUserId))

            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.RuntimeId)
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