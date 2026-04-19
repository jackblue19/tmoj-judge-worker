using Application.UseCases.Favorite.Commands.AddContestToCollection;
using Application.UseCases.Favorite.Commands.AddProblemToCollection;
using Application.UseCases.Favorite.Commands.CopyCollection;
using Application.UseCases.Favorite.Commands.CreateCollection;
using Application.UseCases.Favorite.Commands.DeleteCollection;
using Application.UseCases.Favorite.Commands.RemoveCollectionItem;
using Application.UseCases.Favorite.Commands.ReorderCollectionItems;
using Application.UseCases.Favorite.Commands.ToggleFavoriteContest;
using Application.UseCases.Favorite.Commands.ToggleFavoriteProblem;
using Application.UseCases.Favorite.Commands.UpdateCollection;
using Application.UseCases.Favorite.Dtos;
using Application.UseCases.Favorite.Queries.CheckFavorite;
using Application.UseCases.Favorite.Queries.GetCollectionDetail;
using Application.UseCases.Favorite.Queries.GetFavoriteContests;
using Application.UseCases.Favorite.Queries.GetFavoriteProblems;
using Application.UseCases.Favorite.Queries.GetMyCollections;
using Application.UseCases.Favorite.Queries.GetPublicCollections;
using Application.UseCases.Favorite.Queries.GetUserPublicCollections;
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

    // =========================
    // TOGGLE PROBLEM
    // =========================
    [HttpPost("problems/{problemId}/toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleFavoriteProblem(Guid problemId)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new ToggleFavoriteProblemCommand
        {
            UserId = userId,
            ProblemId = problemId
        });

        return Ok(new
        {
            data = result,
            message = result.IsSuccess
                ? "Toggle favorite problem successfully"
                : result.ErrorMessage,
            traceId = HttpContext.TraceIdentifier
        });
    }

    // =========================
    // CHECK FAVORITE
    // =========================
    [HttpGet("check")]
    [Authorize]
    public async Task<IActionResult> CheckFavorite(
        [FromQuery] Guid? problemId,
        [FromQuery] Guid? contestId)
    {
        var userId = GetUserId();

        if (problemId == null && contestId == null)
        {
            return BadRequest(new
            {
                message = "Must provide problemId or contestId",
                traceId = HttpContext.TraceIdentifier
            });
        }

        var result = await _mediator.Send(new CheckFavoriteQuery
        {
            UserId = userId,
            ProblemId = problemId,
            ContestId = contestId
        });

        return Ok(new
        {
            data = result,
            message = "Check favorite successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }

    // =========================
    // TOGGLE CONTEST
    // =========================
    [HttpPost("contests/{contestId}/toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleFavoriteContest(Guid contestId)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new ToggleFavoriteContestCommand
        {
            UserId = userId,
            ContestId = contestId
        });

        return Ok(new
        {
            data = result,
            message = result.IsSuccess
                ? "Toggle favorite contest successfully"
                : result.ErrorMessage,
            traceId = HttpContext.TraceIdentifier
        });
    }

    // =========================
    // GET FAVORITE PROBLEMS
    // =========================
    [HttpGet("problems")]
    [Authorize]
    public async Task<IActionResult> GetFavoriteProblems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new GetFavoriteProblemsQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize
        });

        return Ok(new
        {
            data = result.Items,
            pagination = new
            {
                totalItems = result.TotalItems,
                totalPages = result.TotalPages,
                page = result.Page,
                pageSize = result.PageSize
            },
            message = "Get favorite problems successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }

    // =========================
    // GET FAVORITE CONTESTS
    // =========================
    [HttpGet("contests")]
    [Authorize]
    public async Task<IActionResult> GetFavoriteContests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new GetFavoriteContestsQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize
        });

        return Ok(new
        {
            data = result.Items,
            pagination = new
            {
                totalItems = result.TotalItems,
                totalPages = result.TotalPages,
                page = result.Page,
                pageSize = result.PageSize
            },
            message = "Get favorite contests successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }

    // =========================
    // COLLECTION APIs (FIXED ROUTE)
    // =========================

    [HttpPost("collections")]
    [Authorize]
    public async Task<IActionResult> CreateCollection(
        [FromBody] CreateCollectionRequest request)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new CreateCollectionCommand
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            IsVisibility = request.IsVisibility
        });

        return Ok(new
        {
            data = result,
            message = result.IsSuccess
                ? "Create collection successfully"
                : result.ErrorMessage,
            traceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("collections/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateCollection(Guid id, [FromBody] UpdateCollectionCommand body)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new UpdateCollectionCommand
        {
            Id = id,
            UserId = userId,
            Name = body.Name,
            Description = body.Description,
            IsVisibility = body.IsVisibility
        });

        return Ok(new
        {
            data = result,
            message = result.IsSuccess
                ? "Update collection successfully"
                : result.ErrorMessage,
            traceId = HttpContext.TraceIdentifier
        });
    }

    [HttpDelete("collections/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteCollection(Guid id)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new DeleteCollectionCommand
        {
            Id = id,
            UserId = userId
        });

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                error = result.ErrorCode,
                message = result.ErrorMessage,
                traceId = HttpContext.TraceIdentifier
            });
        }

        return NoContent();
    }

    [HttpGet("collections")]
    [Authorize]
    public async Task<IActionResult> GetMyCollections()
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new GetMyCollectionsQuery
        {
            UserId = userId
        });

        return Ok(new
        {
            data = result,
            message = "Get my collections successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("collections/{id}")]
    [Authorize]
    public async Task<IActionResult> GetCollectionDetail(Guid id)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new GetCollectionDetailQuery
        {
            UserId = userId,
            CollectionId = id
        });

        return Ok(new
        {
            data = result,
            message = "Get collection detail successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }

    private Guid GetUserId()
    {
        var userIdClaim =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Missing user claim");

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user id");

        return userId;
    }

    // =========================
    // ADD PROBLEM TO COLLECTION
    // =========================
    [HttpPost("/api/v1/collections/{id}/problems")]
    [Authorize]
    public async Task<IActionResult> AddProblemToCollection(
        Guid id,
        [FromBody] AddProblemToCollectionRequest request)
    {
        Console.WriteLine("🔥 API AddProblemToCollection called");

        try
        {
            var userId = GetUserId();

            var result = await _mediator.Send(new AddProblemToCollectionCommand
            {
                UserId = userId,
                CollectionId = id,
                ProblemId = request.ProblemId
            });

            return Ok(new
            {
                data = new
                {
                    result.CollectionId,
                    result.ProblemId,
                    result.ItemId,
                    result.IsSuccess,
                    result.IsAlreadyExists
                },
                message = result.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new
            {
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR AddProblemToCollection: {ex.Message}");

            return StatusCode(500, new
            {
                message = "Internal server error",
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    // =========================
    // ADD CONTEST TO COLLECTION
    // =========================
    [HttpPost("/api/v1/collections/{id}/contests")]
    [Authorize]
    public async Task<IActionResult> AddContestToCollection(
        Guid id,
        [FromBody] AddContestToCollectionRequest request)
    {
        Console.WriteLine("🔥 API AddContestToCollection called");

        try
        {
            var userId = GetUserId();

            var result = await _mediator.Send(new AddContestToCollectionCommand
            {
                UserId = userId,
                CollectionId = id,
                ContestId = request.ContestId
            });

            return Ok(new
            {
                data = new
                {
                    result.CollectionId,
                    result.ContestId,
                    result.ItemId,
                    result.IsSuccess,
                    result.IsAlreadyExists
                },
                message = result.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new
            {
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR AddContestToCollection: {ex.Message}");

            return StatusCode(500, new
            {
                message = "Internal server error",
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
    // =========================
    // REMOVE ITEM FROM COLLECTION
    // =========================
    [HttpDelete("/api/v1/collections/{id}/items/{itemId}")]
    [Authorize]
    public async Task<IActionResult> RemoveItemFromCollection(
        Guid id,
        Guid itemId)
    {
        Console.WriteLine("🔥 API RemoveItemFromCollection called");

        try
        {
            var userId = GetUserId();

            var result = await _mediator.Send(new RemoveCollectionItemCommand
            {
                UserId = userId,
                CollectionId = id,
                ItemId = itemId
            });

            if (!result)
            {
                return NotFound(new
                {
                    message = "Item not found",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent(); // ✅ đúng spec
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new
            {
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR RemoveItem: {ex.Message}");

            return StatusCode(500, new
            {
                message = "Internal server error",
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
    // =========================
    // REORDER COLLECTION ITEMS
    // =========================
    [HttpPut("/api/v1/collections/{id}/reorder")]
    [Authorize]
    public async Task<IActionResult> ReorderCollectionItems(
        Guid id,
        [FromBody] ReorderCollectionRequest request)
    {
        Console.WriteLine("🔥 API ReorderCollectionItems called");

        try
        {
            var userId = GetUserId();

            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest(new
                {
                    message = "Items list cannot be empty",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var result = await _mediator.Send(new ReorderCollectionItemsCommand
            {
                UserId = userId,
                CollectionId = id,

                // ✅ dùng thẳng DTO
                Items = request.Items
            });

            return Ok(new
            {
                data = new
                {
                    collectionId = id,
                    totalItems = request.Items.Count,
                    updatedAt = DateTime.UtcNow
                },
                message = "Reordered items successfully",
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new
            {
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR Reorder: {ex.Message}");

            return StatusCode(500, new
            {
                message = "Internal server error",
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
    // =========================
    // GET PUBLIC COLLECTIONS
    // =========================
    [HttpGet("/api/v1/collections/public")]
    public async Task<IActionResult> GetPublicCollections(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 50) pageSize = 10;

        var result = await _mediator.Send(new GetPublicCollectionsQuery
        {
            Page = page,
            PageSize = pageSize
        });

        return Ok(new
        {
            data = result.Items,
            pagination = new
            {
                totalItems = result.TotalItems,
                totalPages = result.TotalPages,
                page = result.Page,
                pageSize = result.PageSize
            },
            message = "Get public collections successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }
    // =========================
    // GET USER PUBLIC COLLECTIONS
    // =========================
    [HttpGet("/api/v1/users/{userId}/collections")]
    public async Task<IActionResult> GetUserPublicCollections(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 50) pageSize = 10;

        var result = await _mediator.Send(new GetUserPublicCollectionsQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize
        });

        return Ok(new
        {
            data = result.Items,
            pagination = new
            {
                totalItems = result.TotalItems,
                totalPages = result.TotalPages,
                page = result.Page,
                pageSize = result.PageSize
            },
            message = "Get user public collections successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }
    // =========================
    // COPY COLLECTION
    // =========================
    [HttpPost("/api/v1/collections/{id}/copy")]
    [Authorize]
    public async Task<IActionResult> CopyCollection(Guid id)
    {
        Console.WriteLine("🔥 API CopyCollection called");

        try
        {
            var userId = GetUserId();

            var result = await _mediator.Send(new CopyCollectionCommand
            {
                UserId = userId,
                SourceCollectionId = id
            });

            return Ok(new
            {
                data = new
                {
                    result.NewCollectionId,
                    result.TotalItems
                },
                message = result.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new
            {
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR CopyCollection: {ex.Message}");

            return StatusCode(500, new
            {
                message = "Internal server error",
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}