using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemTemplates.Specifications;

public sealed class ProblemTemplateByProblemRuntimeVersionSpec : Specification<ProblemTemplate>
{
    public ProblemTemplateByProblemRuntimeVersionSpec(Guid problemId , Guid runtimeId , int version)
    {
        Query
            .Where(x =>
                x.ProblemId == problemId &&
                x.RuntimeId == runtimeId &&
                x.Version == version)
            .AsNoTracking();
    }
}