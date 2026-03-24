using Application.UseCases.Editorials;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class EditorialController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EditorialController(IMediator mediator)
        {
            _mediator = mediator;
        }


        /// <summary>
        /// Get editorials by problemId (with cursor pagination)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] Guid problemId,
            [FromQuery] Guid? cursorId,
            [FromQuery] DateTime? cursorCreatedAt,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            // 🔥 validate basic
            if (problemId == Guid.Empty)
            {
                return BadRequest(new { Message = "problemId is required" });
            }

            if (pageSize <= 0 || pageSize > 50)
            {
                return BadRequest(new { Message = "pageSize must be between 1 and 50" });
            }

            var result = await _mediator.Send(
                new ViewEditorialQuery(
                    problemId,
                    cursorId,
                    cursorCreatedAt,
                    pageSize
                ),
                ct
            );

            return Ok(result);
     
            }
        [HttpPost]
        public async Task<IActionResult> Create(
         [FromBody] CreateEditorialCommand cmd,
         CancellationToken ct)
        {
            if (cmd == null)
                return BadRequest(new { Message = "Invalid request" });

            var id = await _mediator.Send(cmd, ct);

            return Ok(new
            {
                EditorialId = id
            });
        }

    }




}
