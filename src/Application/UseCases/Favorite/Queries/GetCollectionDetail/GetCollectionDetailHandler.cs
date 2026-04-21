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
        Console.WriteLine("🔥 GetCollectionDetail START");

        var userId = request.UserId;
        var collectionId = request.CollectionId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        if (collectionId == Guid.Empty)
            throw new Exception("CollectionId invalid");

        // =========================
        // GET COLLECTION
        // =========================
        var collection = await _repo.GetCollectionByIdAsync(collectionId);

        if (collection == null)
            throw new Exception("Collection not found");

        // =========================
        // PERMISSION
        // =========================
        if (!collection.IsVisibility && collection.UserId != userId)
            throw new Exception("You do not have permission to view this collection");

        // =========================
        // GET ITEMS
        // =========================
        var items = await _repo.GetCollectionItemsDetailAsync(collectionId);

        // =========================
        // COUNT
        // =========================
        var problemIds = items
            .Where(x => x.ProblemId != null)
            .Select(x => x.ProblemId!.Value)
            .ToList();

        var problemCount = problemIds.Count;
        var contestCount = items.Count(x => x.ContestId != null);

        // =========================
        // SOLVED
        // =========================
        var solvedSet = await _repo.GetSolvedProblemIdsAsync(userId, problemIds);
        var solvedCount = solvedSet.Count;

        // =========================
        // MAP
        // =========================
        var result = new CollectionDetailDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            Type = collection.Type,
            IsVisibility = collection.IsVisibility,

            TotalItems = items.Count,
            ProblemCount = problemCount,
            ContestCount = contestCount,
            SolvedProblems = solvedCount,

            Items = items.Select(x => new CollectionItemDto
            {
                Id = x.Id,

                ProblemId = x.ProblemId,
                ProblemTitle = x.Problem?.Title,

                // ✅ FIX CHÍNH
                ProblemDifficulty = x.Problem?.Difficulty,

                ContestId = x.ContestId,
                ContestTitle = x.Contest?.Title,

                CreatedAt = x.CreatedAt,

                IsSolved = x.ProblemId != null &&
                           solvedSet.Contains(x.ProblemId.Value)
            }).ToList()
        };

        Console.WriteLine(
            $"✅ Total: {items.Count} | Problems: {problemCount} | Contests: {contestCount} | Solved: {solvedCount}");

        return result;
    }
}