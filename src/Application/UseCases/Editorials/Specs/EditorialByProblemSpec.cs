
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs;

public class EditorialByProblemSpec : Specification<Editorial>
{
    public EditorialByProblemSpec(Guid problemId)
    {
        Query.Where(e => e.ProblemId == problemId);
    }
}

public class EditorialByStorageSpec : Specification<Editorial>
{
    public EditorialByStorageSpec(Guid storageId)
    {
        Query.Where(e => e.StorageId == storageId);
    }
}

public class ProblemByIdSpec : Specification<Problem>
{
    public ProblemByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id);
    }
}

public class StorageByIdSpec : Specification<StorageFile>
{
    public StorageByIdSpec(Guid id)
    {
        Query.Where(s => s.StorageId == id);
    }
}

