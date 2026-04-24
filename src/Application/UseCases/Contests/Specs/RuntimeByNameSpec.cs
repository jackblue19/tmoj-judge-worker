using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class RuntimeByNameSpec : Specification<Runtime>
{
    public RuntimeByNameSpec(string runtimeName)
    {
        Query.Where(r => r.RuntimeName.ToLower() == runtimeName.ToLower());
    }
}
