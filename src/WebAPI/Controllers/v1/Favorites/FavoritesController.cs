using Application.UseCases.Favorite.Commands.ToggleFavoriteProblem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.v1.Favorites;

[ApiController]
[Route("api/v1/favorites")]
[Tags("Favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("problems/{problemId}/toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleFavoriteProblem(Guid problemId)
    {
        Console.WriteLine("🔥 API ToggleFavorite called");

        // =========================
        // GET USER ID (SAFE)
        // =========================
        var userIdClaim =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            Console.WriteLine("❌ Missing user claim");
            return Unauthorized("Missing user claim");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            Console.WriteLine("❌ Invalid userId format");
            return Unauthorized("Invalid user id");
        }

        Console.WriteLine($"👉 UserId: {userId}");
        Console.WriteLine($"👉 ProblemId: {problemId}");

        var result = await _mediator.Send(new ToggleFavoriteProblemCommand
        {
            UserId = userId,
            ProblemId = problemId
        });

        return Ok(new
        {
            isFavorited = result
        });
    }
}