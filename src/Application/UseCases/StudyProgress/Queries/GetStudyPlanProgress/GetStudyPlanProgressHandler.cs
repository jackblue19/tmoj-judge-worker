using Application.Common.Interfaces;
using Application.UseCases.StudyProgress.Dtos;
using MediatR;

namespace Application.UseCases.StudyProgress.Queries.GetStudyPlanProgress;

public class GetStudyPlanProgressHandler
    : IRequestHandler<GetStudyPlanProgressQuery, StudyPlanProgressDto>
{
    private readonly IStudyPlanRepository _repo;

    public GetStudyPlanProgressHandler(IStudyPlanRepository repo)
    {
        _repo = repo;
    }

    public async Task<StudyPlanProgressDto> Handle(
        GetStudyPlanProgressQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _repo.GetItemsByPlanIdAsync(request.StudyPlanId);

        var progresses = await _repo.GetItemProgressByPlanAsync(
            request.UserId,
            request.StudyPlanId
        );

        var progressDict = progresses
            .GroupBy(x => x.StudyPlanItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var itemDtos = items
            .OrderBy(x => x.OrderIndex)
            .Select(i =>
            {
                progressDict.TryGetValue(i.Id, out var p);

                return new StudyPlanItemProgressDto
                {
                    StudyPlanItemId = i.Id,
                    IsCompleted = p?.IsCompleted ?? false,
                    CompletedAt = p?.CompletedAt
                };
            })
            .ToList();

        var total = items.Count;
        var completed = itemDtos.Count(x => x.IsCompleted);

        return new StudyPlanProgressDto
        {
            StudyPlanId = request.StudyPlanId,
            UserId = request.UserId,
            TotalItems = total,
            CompletedItems = completed,
            ProgressPercent = total == 0
                ? 0
                : (double)completed / total * 100,
            Items = itemDtos
        };
    }
}