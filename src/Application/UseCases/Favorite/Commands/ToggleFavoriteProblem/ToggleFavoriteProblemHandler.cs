using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Favorite.Commands.ToggleFavoriteProblem;

public class ToggleFavoriteProblemHandler
    : IRequestHandler<ToggleFavoriteProblemCommand, ToggleFavoriteProblemResponseDto>
{
    private readonly IFavoriteRepository _repo;

    public ToggleFavoriteProblemHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<ToggleFavoriteProblemResponseDto> Handle(
        ToggleFavoriteProblemCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 ToggleFavoriteProblem START");

        var userId = request.UserId;
        var problemId = request.ProblemId;

        if (userId == Guid.Empty)
            return Fail(problemId, "INVALID_USER", "UserId không hợp lệ");

        if (problemId == Guid.Empty)
            return Fail(problemId, "INVALID_PROBLEM", "ProblemId không hợp lệ");

        var problem = await _repo.GetProblemByIdAsync(problemId);

        if (problem == null)
            return Fail(problemId, "NOT_FOUND", "Bài toán không tồn tại");

        if (!problem.IsActive)
            return Fail(problemId, "INACTIVE", "Bài toán đã bị vô hiệu hóa");

        if (problem.VisibilityCode == "private" && problem.CreatedBy != userId)
            return Fail(problemId, "FORBIDDEN", "Bạn không có quyền bookmark bài này");

        var collection = await _repo.GetUserCollectionByTypeAsync(userId, "problem_favorite");

        if (collection == null)
        {
            Console.WriteLine("👉 Create collection");

            collection = new Collection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Favorite Problems",
                Type = "problem_favorite",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(collection);
            await _repo.SaveChangesAsync();
        }

        var item = await _repo.GetCollectionItemAsync(collection.Id, problemId, null);

        // =========================
        // REMOVE
        // =========================
        if (item != null)
        {
            Console.WriteLine("👉 REMOVE");

            await _repo.RemoveItemAsync(item.Id);
            await _repo.SaveChangesAsync();

            return new ToggleFavoriteProblemResponseDto
            {
                ProblemId = problemId,
                IsFavorited = false,
                Action = "removed",
                IsSuccess = true,
                Collection = MapCollection(collection)
            };
        }

        // =========================
        // ADD
        // =========================
        Console.WriteLine("👉 ADD");

        var items = await _repo.GetCollectionItemsByCollectionId(collection.Id);

        int nextOrderIndex = items.Any()
            ? items.Max(x => x.OrderIndex) + 1
            : 1;

        var newItem = new CollectionItem
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            ProblemId = problemId,
            OrderIndex = nextOrderIndex, // 🔥 FIX
            CreatedAt = DateTime.UtcNow
        };

        var inserted = await _repo.TryAddItemAsync(newItem);

        if (!inserted)
        {
            Console.WriteLine("⚠️ Race condition");

            return new ToggleFavoriteProblemResponseDto
            {
                ProblemId = problemId,
                IsFavorited = true,
                Action = "added",
                IsSuccess = true,
                Collection = MapCollection(collection)
            };
        }

        return new ToggleFavoriteProblemResponseDto
        {
            ProblemId = problemId,
            IsFavorited = true,
            Action = "added",
            IsSuccess = true,
            Collection = MapCollection(collection)
        };
    }

    private ToggleFavoriteProblemResponseDto Fail(Guid id, string code, string msg)
    {
        return new ToggleFavoriteProblemResponseDto
        {
            ProblemId = id,
            IsSuccess = false,
            ErrorCode = code,
            ErrorMessage = msg,
            Action = "none",
            IsFavorited = false
        };
    }

    private CollectionInfoDto MapCollection(Collection c)
    {
        return new CollectionInfoDto
        {
            Id = c.Id,
            Type = c.Type,
            Name = c.Name
        };
    }
}