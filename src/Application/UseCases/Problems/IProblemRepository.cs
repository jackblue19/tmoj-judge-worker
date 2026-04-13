using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using Domain.Entities;

namespace Application.UseCases.Problems;

public interface IProblemRepository : IWriteRepository<Problem , Guid>, IReadRepository<Problem , Guid>
{
    Task<bool> SlugExistsAsync(string slug , Guid? excludingProblemId , CancellationToken ct = default);

    Task<Problem?> GetProblemForManagementAsync(
        Guid problemId ,
        Guid currentUserId ,
        bool isAdmin ,
        CancellationToken ct = default);

    Task<ProblemDetailDto?> GetProblemDetailForManagementAsync(
        Guid problemId ,
        Guid currentUserId ,
        bool isAdmin ,
        CancellationToken ct = default);

    Task<Problem?> GetProblemTrackedWithTagsAsync(Guid problemId , CancellationToken ct = default);

    Task<Problem?> GetProblemTrackedWithTagsAndTestsetsAsync(Guid problemId , CancellationToken ct = default);

    Task<IReadOnlyList<Tag>> GetTagsTrackedByIdsAsync(
        IReadOnlyCollection<Guid> tagIds ,
        CancellationToken ct = default);
}