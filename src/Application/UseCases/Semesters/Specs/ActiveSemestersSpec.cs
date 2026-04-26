using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Semesters.Specs;

public class ActiveSemestersSpec : Specification<Semester>
{
    public ActiveSemestersSpec(string? search = null, int page = 1, int pageSize = 20)
    {
        Query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            Query.Where(x =>
                x.Code.ToLower().Contains(searchLower) ||
                x.Name.ToLower().Contains(searchLower));
        }

        Query.OrderByDescending(x => x.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}

public class ActiveSemestersSearchSpec : Specification<Semester>
{
    public ActiveSemestersSearchSpec(string? search = null)
    {
        Query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            Query.Where(x =>
                x.Code.ToLower().Contains(searchLower) ||
                x.Name.ToLower().Contains(searchLower));
        }
    }
}

public class AllSemestersSpec : Specification<Semester>
{
    public AllSemestersSpec(string? search = null, int page = 1, int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            Query.Where(x =>
                x.Code.ToLower().Contains(searchLower) ||
                x.Name.ToLower().Contains(searchLower));
        }

        Query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}

public class AllSemestersSearchSpec : Specification<Semester>
{
    public AllSemestersSearchSpec(string? search = null)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            Query.Where(x =>
                x.Code.ToLower().Contains(searchLower) ||
                x.Name.ToLower().Contains(searchLower));
        }
    }
}
