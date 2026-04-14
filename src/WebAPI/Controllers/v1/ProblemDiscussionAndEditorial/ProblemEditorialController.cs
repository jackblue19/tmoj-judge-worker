using Application.UseCases.Editorials.Commands;
using Application.UseCases.ProblemEditorials.Commands;
using Application.UseCases.ProblemEditorials.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/problem-editorials")]
    public class ProblemEditorialController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProblemEditorialController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // =========================
        // GET LIST
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] Guid problemId,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (problemId == Guid.Empty)
                    return BadRequest(new { success = false, message = "problemId is required" });

                if (pageSize <= 0 || pageSize > 50)
                    return BadRequest(new { success = false, message = "pageSize must be between 1 and 50" });

                var result = await _mediator.Send(new GetProblemEditorialsQuery
                {
                    ProblemId = problemId,
                    PageSize = pageSize
                });

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Fetch failed",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest(new { success = false, message = "id is required" });

                var result = await _mediator.Send(new GetProblemEditorialByIdQuery
                {
                    Id = id
                });

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Get failed",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // =========================
        // CREATE
        // =========================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProblemEditorialCommand command)
        {
            try
            {
                var id = await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = "Editorial created successfully",
                    data = new { id }
                });
            }
            catch (Exception ex)
            {
                // 🔥 BUSINESS ERROR
                if (ex.Message == "EDITORIAL_ALREADY_EXISTS")
                {
                    return Conflict(new
                    {
                        success = false,
                        errorCode = "EDITORIAL_EXISTS",
                        message = "This problem already has an editorial.",
                        suggestion = "Use PUT /problem-editorials/{id} to update instead.",
                        debug = new
                        {
                            problemId = command.ProblemId
                        }
                    });
                }

                // 🔥 UNKNOWN ERROR
                return StatusCode(500, new
                {
                    success = false,
                    errorCode = "INTERNAL_ERROR",
                    message = "Create editorial failed",
                    detail = ex.Message,
                    innerError = ex.InnerException?.Message,
                    trace = ex.StackTrace
                });
            }
        }
        // =========================
        // UPDATE
        // =========================
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProblemEditorialCommand command)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest(new { success = false, message = "id is required" });

                if (command == null)
                    return BadRequest(new { success = false, message = "Request body is required" });

                if (string.IsNullOrWhiteSpace(command.Content))
                    return BadRequest(new { success = false, message = "content is required" });

                command.Id = id;

                var result = await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = "Updated successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Update failed",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // =========================
        // DELETE
        // =========================
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest(new { success = false, message = "id is required" });

                await _mediator.Send(new DeleteProblemEditorialCommand { Id = id });

                return Ok(new
                {
                    success = true,
                    message = "Deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Delete failed",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}