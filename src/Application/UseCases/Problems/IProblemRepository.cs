using Application.UseCases.Problems.Dtos;
using Domain.Entities;

namespace Application.UseCases.Problems;

public interface IProblemRepository
{
    Task<bool> SlugExistsAsync(string slug, Guid? excludingProblemId, CancellationToken ct = default);

    Task<Problem?> GetProblemForManagementAsync(
        Guid problemId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken ct = default);

    Task<ProblemDetailDto?> GetProblemDetailForManagementAsync(
        Guid problemId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken ct = default);
}
