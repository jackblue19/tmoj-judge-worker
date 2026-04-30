using Application.UseCases.AI.Dtos;

namespace Application.Abstractions.AI;

public interface IAiDebugDataService
{
    Task<AiDebugContext?> GetDebugContextAsync(
        Guid submissionId ,
        Guid? resultId ,
        Guid currentUserId ,
        CancellationToken ct = default);

    Task<AiDebugCachedResult?> GetCachedDebugAsync(
        Guid submissionId ,
        Guid? resultId ,
        string contextHash ,
        CancellationToken ct = default);

    Task<int> CountTodayRequestsAsync(
        Guid userId ,
        string featureCode ,
        CancellationToken ct = default);

    Task<Guid> InsertRequestLogAsync(
        AiRequestLogCreateDto dto ,
        CancellationToken ct = default);

    Task<Guid> InsertDebugSessionAsync(
        AiDebugSessionCreateDto dto ,
        CancellationToken ct = default);
}