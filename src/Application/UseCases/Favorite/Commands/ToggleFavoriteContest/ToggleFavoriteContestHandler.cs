using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Favorite.Commands.ToggleFavoriteContest;

public class ToggleFavoriteContestHandler
    : IRequestHandler<ToggleFavoriteContestCommand, ToggleFavoriteContestResponseDto>
{
    private readonly IFavoriteRepository _repo;

    public ToggleFavoriteContestHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<ToggleFavoriteContestResponseDto> Handle(
        ToggleFavoriteContestCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 ToggleFavoriteContest START");

        var userId = request.UserId;
        var contestId = request.ContestId;

        if (userId == Guid.Empty)
            return Fail(contestId, "INVALID_USER", "UserId không hợp lệ");

        if (contestId == Guid.Empty)
            return Fail(contestId, "INVALID_CONTEST", "ContestId không hợp lệ");

        var contest = await _repo.GetContestByIdAsync(contestId);

        if (contest == null)
            return Fail(contestId, "NOT_FOUND", "Contest không tồn tại");

        if (!contest.IsActive)
            return Fail(contestId, "INACTIVE", "Contest đã bị vô hiệu hóa");

        if (contest.VisibilityCode == "private" && contest.CreatedBy != userId)
            return Fail(contestId, "FORBIDDEN", "Bạn không có quyền bookmark contest này");

        var collection = await _repo.GetUserCollectionByTypeAsync(userId, "contest_favorite");

        if (collection == null)
        {
            Console.WriteLine("👉 Create collection");

            collection = new Collection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "contest_favorite",
                Name = "Favorite Contests",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(collection);
            await _repo.SaveChangesAsync();
        }

        var item = await _repo.GetCollectionItemAsync(collection.Id, null, contestId);

        // =========================
        // REMOVE
        // =========================
        if (item != null)
        {
            Console.WriteLine("👉 REMOVE");

            await _repo.RemoveItemAsync(item.Id);
            await _repo.SaveChangesAsync();

            return new ToggleFavoriteContestResponseDto
            {
                ContestId = contestId,
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
            ContestId = contestId,
            OrderIndex = nextOrderIndex, // 🔥 FIX
            CreatedAt = DateTime.UtcNow
        };

        var inserted = await _repo.TryAddItemAsync(newItem);

        if (!inserted)
        {
            Console.WriteLine("⚠️ Race condition");

            return new ToggleFavoriteContestResponseDto
            {
                ContestId = contestId,
                IsFavorited = true,
                Action = "added",
                IsSuccess = true,
                Collection = MapCollection(collection)
            };
        }

        return new ToggleFavoriteContestResponseDto
        {
            ContestId = contestId,
            IsFavorited = true,
            Action = "added",
            IsSuccess = true,
            Collection = MapCollection(collection)
        };
    }

    private ToggleFavoriteContestResponseDto Fail(Guid id, string code, string msg)
    {
        return new ToggleFavoriteContestResponseDto
        {
            ContestId = id,
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