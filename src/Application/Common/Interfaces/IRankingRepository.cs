using Application.UseCases.Ranking.Dtos;

namespace Application.Common.Interfaces;

public interface IRankingRepository
{
    Task<GlobalLeaderboardDto> GetGlobalLeaderboardAsync(
        int page, int pageSize, string? search,
        Guid? subjectId, Guid? semesterId,
        CancellationToken ct = default);

    Task<List<PublicContestSummaryDto>> GetPublicContestsAsync(
        Guid? subjectId, Guid? semesterId,
        CancellationToken ct = default);

    Task<bool> IsPublicContestAsync(Guid contestId, CancellationToken ct = default);

    Task<bool> ContestExistsAsync(Guid contestId, CancellationToken ct = default);
}
