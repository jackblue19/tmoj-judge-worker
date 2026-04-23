using Application.UseCases.ProblemTemplates.Commands.CreateProblemTemplate;
using Application.UseCases.ProblemTemplates.Commands.DeleteProblemTemplate;
using Application.UseCases.ProblemTemplates.Commands.UpdateProblemTemplate;
using Application.UseCases.ProblemTemplates.Dtos;
using Application.UseCases.ProblemTemplates.Queries.GetProblemTemplatesByProblemId;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v2.ProblemManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public partial class ProblemTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProblemTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{problemId:guid}/templates")]
    public async Task<ActionResult<ApiResponse<ProblemTemplateDto>>> CreateTemplate(
        Guid problemId ,
        [FromBody] CreateProblemTemplateRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemTemplateCommand(
                problemId ,
                request.RuntimeId ,
                request.TemplateCode ,
                request.InjectionPoint ,
                request.SolutionSignature ,
                request.Version) ,
            ct);

        return Ok(
            ApiResponse<ProblemTemplateDto>.Ok(
                result ,
                "Problem template created successfully." ,
                HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpGet("{problemId:guid}/templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProblemTemplateDto>>>> GetTemplates(
         Guid problemId ,
         CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetProblemTemplatesByProblemIdQuery(problemId) ,
            ct);

        return Ok(
            ApiResponse<IReadOnlyList<ProblemTemplateDto>>.Ok(
                result ,
                "Problem templates fetched successfully." ,
                HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("templates/{codeTemplateId:guid}")]
    public async Task<ActionResult<ApiResponse<ProblemTemplateDto>>> UpdateTemplate(
        Guid codeTemplateId ,
        [FromBody] UpdateProblemTemplateRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProblemTemplateCommand(
                codeTemplateId ,
                request.TemplateCode ,
                request.InjectionPoint ,
                request.SolutionSignature ,
                request.IsActive) ,
            ct);

        return Ok(
            ApiResponse<ProblemTemplateDto>.Ok(
                result ,
                "Problem template updated successfully." ,
                HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("templates/{codeTemplateId:guid}")]
    public async Task<ActionResult<ApiResponse<ProblemTemplateDto>>> DeleteTemplate(
       Guid codeTemplateId ,
       CancellationToken ct)
    {
        var result = await _mediator.Send(
            new DeleteProblemTemplateCommand(codeTemplateId) ,
            ct);

        return Ok(
            ApiResponse<ProblemTemplateDto>.Ok(
                result ,
                "Problem template deleted successfully." ,
                HttpContext.TraceIdentifier));
    }
}