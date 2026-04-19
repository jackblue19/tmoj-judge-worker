using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Queries.CheckFavorite;

public class CheckFavoriteHandler
    : IRequestHandler<CheckFavoriteQuery, CheckFavoriteResponseDto>
{
    private readonly IFavoriteRepository _repo;

    public CheckFavoriteHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<CheckFavoriteResponseDto> Handle(
        CheckFavoriteQuery request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 CheckFavorite START");

        var userId = request.UserId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        if (request.ProblemId == null && request.ContestId == null)
            throw new Exception("Must provide problemId or contestId");

        var isFavorited = await _repo.IsFavoritedAsync(
            userId,
            request.ProblemId,
            request.ContestId
        );

        Console.WriteLine($"👉 isFavorited = {isFavorited}");

        return new CheckFavoriteResponseDto
        {
            IsFavorited = isFavorited,
            Type = request.ProblemId != null ? "problem" : "contest"
        };
    }
}