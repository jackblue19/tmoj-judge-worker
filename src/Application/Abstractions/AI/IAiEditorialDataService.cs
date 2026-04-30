using Application.UseCases.AI.Dtos;

namespace Application.Abstractions.AI;

public interface IAiEditorialDataService
{
    Task<string> GetCurrentUserRoleCodeAsync(
        Guid userId ,
        CancellationToken ct = default);

    Task<AiEditorialContext?> GetEditorialContextAsync(
        Guid problemId ,
        CancellationToken ct = default);

    Task<AiEditorialCachedDraft?> GetCachedDraftAsync(
        Guid problemId ,
        string contextHash ,
        string languageCode ,
        string styleCode ,
        CancellationToken ct = default);

    Task<int> CountTodayRequestsAsync(
        Guid userId ,
        string featureCode ,
        CancellationToken ct = default);

    Task<Guid> InsertRequestLogAsync(
        AiRequestLogCreateDto dto ,
        CancellationToken ct = default);

    Task<Guid> InsertEditorialDraftAsync(
        AiEditorialDraftCreateDto dto ,
        CancellationToken ct = default);
}