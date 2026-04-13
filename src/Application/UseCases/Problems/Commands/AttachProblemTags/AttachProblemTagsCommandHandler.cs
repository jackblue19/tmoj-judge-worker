using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Helpers;
using Application.UseCases.Problems.Mappings;
using Domain.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Problems.Commands.AttachProblemTags;

public sealed class AttachProblemTagsCommandHandler : IRequestHandler<AttachProblemTagsCommand , ProblemDetailDto>
{
    private readonly IProblemRepository _problemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AttachProblemTagsCommandHandler> _logger;

    public AttachProblemTagsCommandHandler(
        IProblemRepository problemRepository ,
        IUnitOfWork unitOfWork ,
        ILogger<AttachProblemTagsCommandHandler> logger)
    {
        _problemRepository = problemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProblemDetailDto> Handle(AttachProblemTagsCommand request , CancellationToken ct)
    {
        var problem = await _problemRepository.GetProblemTrackedWithTagsAndTestsetsAsync(request.ProblemId , ct);

        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        var normalizedTagIds = ProblemCommandGuard.NormalizeTagIds(request.TagIds);

        if ( normalizedTagIds.Count == 0 )
            return problem.ToDetailDto();

        var tags = await _problemRepository.GetTagsTrackedByIdsAsync(normalizedTagIds , ct);

        var foundIds = tags.Select(x => x.Id).ToHashSet();
        var missingIds = normalizedTagIds.Where(x => !foundIds.Contains(x)).ToArray();
        if ( missingIds.Length > 0 )
            throw new KeyNotFoundException($"Some tags were not found: {string.Join(", " , missingIds)}");

        var existingIds = problem.Tags.Select(x => x.Id).ToHashSet();
        var changed = false;

        foreach ( var tag in tags.OrderBy(x => x.Name) )
        {
            if ( existingIds.Add(tag.Id) )
            {
                problem.Tags.Add(tag);
                changed = true;
            }
        }

        if ( !changed )
            return problem.ToDetailDto();

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return problem.ToDetailDto();
        }
        catch ( OperationCanceledException )
        {
            _logger.LogWarning(
                "Attach tags was cancelled. ProblemId={ProblemId}, TagIds={TagIds}" ,
                request.ProblemId ,
                normalizedTagIds);
            throw;
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(ex ,
                "Failed to attach tags to problem. ProblemId={ProblemId}, TagIds={TagIds}" ,
                request.ProblemId ,
                normalizedTagIds);

            throw new InvalidOperationException("Failed to attach tags to problem.");
        }
    }
}