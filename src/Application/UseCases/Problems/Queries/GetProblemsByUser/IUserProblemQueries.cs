using Application.Common.Pagination;

namespace Application.UseCases.Problems.Queries.GetProblemsByUser;

public interface IUserProblemQueries
{
    Task<ApiPagedResponse<ProblemByUserListItemDto>> GetProblemsByUserAsync(
        Guid userId ,
        int page ,
        int pageSize ,
        string? traceId ,
        CancellationToken ct = default);
}