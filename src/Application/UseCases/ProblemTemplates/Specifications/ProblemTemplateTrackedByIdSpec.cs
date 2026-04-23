using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemTemplates.Specifications;

public sealed class ProblemTemplateTrackedByIdSpec : Specification<ProblemTemplate>
{
    public ProblemTemplateTrackedByIdSpec(Guid codeTemplateId)
    {
        Query
            .Where(x => x.CodeTemplateId == codeTemplateId)
            .Include(x => x.Problem);
    }
}