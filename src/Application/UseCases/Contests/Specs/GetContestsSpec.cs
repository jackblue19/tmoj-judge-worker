using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class GetContestsSpec : Specification<Contest>
{
    public GetContestsSpec(string? status, int page, int pageSize)
    {
        var now = DateTime.UtcNow;

        // ===============================
        // FILTER STATUS
        // ===============================
        if (!string.IsNullOrEmpty(status))
        {
            status = status.ToLower();

            if (status == "upcoming")
            {
                Query.Where(x => x.StartAt > now);
            }
            else if (status == "running")
            {
                Query.Where(x => x.StartAt <= now && x.EndAt >= now);
            }
            else if (status == "ended")
            {
                Query.Where(x => x.EndAt < now);
            }
        }

        // ===============================
        // ORDER + PAGINATION
        // ===============================
        Query
            .OrderByDescending(x => x.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}