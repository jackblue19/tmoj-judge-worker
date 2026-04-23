using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Queries.GetCollectionDetail;

public class GetCollectionDetailHandler
    : IRequestHandler<GetCollectionDetailQuery, CollectionDetailDto>
{
    private readonly IFavoriteRepository _repo;

    public GetCollectionDetailHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<CollectionDetailDto> Handle(
        GetCollectionDetailQuery request,
        CancellationToken ct)
    {
        var userId = request.UserId;
        var collectionId = request.CollectionId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        if (collectionId == Guid.Empty)
            throw new Exception("CollectionId invalid");

        var collection = await _repo.GetCollectionByIdAsync(collectionId);

        if (collection == null)
            throw new Exception("Collection not found");

        if (!collection.IsVisibility && collection.UserId != userId)
            throw new Exception("No permission");

        var items = await _repo.GetCollectionItemsDetailAsync(collectionId);

        var problemIds = items
            .Where(x => x.ProblemId != null)
            .Select(x => x.ProblemId!.Value)
            .Distinct()
            .ToList();

        var problemCount = problemIds.Count;
        var contestCount = items.Count(x => x.ContestId != null);

        var solvedSet = await _repo.GetSolvedProblemIdsAsync(userId, problemIds);

        return new CollectionDetailDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            Type = collection.Type,
            IsVisibility = collection.IsVisibility,

            TotalItems = items.Count,
            ProblemCount = problemCount,
            ContestCount = contestCount,
            SolvedProblems = solvedSet.Count,

            Items = items.Select(x => new CollectionItemDto
            {
                Id = x.Id,

                ProblemId = x.ProblemId,
                ProblemTitle = x.Problem?.Title,
                ProblemDifficulty = x.Problem?.Difficulty,

                ContestId = x.ContestId,
                ContestTitle = x.Contest?.Title,

                CreatedAt = x.CreatedAt,

                IsSolved = x.ProblemId != null &&
                           solvedSet.Contains(x.ProblemId.Value),
                           
                IsPrivate = x.Problem != null && x.Problem.VisibilityCode != "public" // ✅ Map cờ IsPrivate
            }).ToList()
        };
    }
}