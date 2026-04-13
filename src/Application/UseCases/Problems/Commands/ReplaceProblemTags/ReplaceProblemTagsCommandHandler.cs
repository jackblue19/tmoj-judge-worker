using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Helpers;
using Application.UseCases.Problems.Mappings;
using Domain.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Problems.Commands.ReplaceProblemTags;

public sealed class ReplaceProblemTagsCommandHandler : IRequestHandler<ReplaceProblemTagsCommand , ProblemDetailDto>
{
    private readonly IProblemRepository _problemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReplaceProblemTagsCommandHandler> _logger;

    public ReplaceProblemTagsCommandHandler(
        IProblemRepository problemRepository ,
        IUnitOfWork unitOfWork ,
        ILogger<ReplaceProblemTagsCommandHandler> logger)
    {
        _problemRepository = problemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProblemDetailDto> Handle(ReplaceProblemTagsCommand request , CancellationToken ct)
    {
        var problem = await _problemRepository.GetProblemTrackedWithTagsAndTestsetsAsync(request.ProblemId , ct);

        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        var normalizedTagIds = ProblemCommandGuard.NormalizeTagIds(request.TagIds);

        var tags = normalizedTagIds.Count == 0
            ? []
            : await _problemRepository.GetTagsTrackedByIdsAsync(normalizedTagIds , ct);

        var foundIds = tags.Select(x => x.Id).ToHashSet();
        var missingIds = normalizedTagIds.Where(x => !foundIds.Contains(x)).ToArray();
        if ( missingIds.Length > 0 )
            throw new KeyNotFoundException($"Some tags were not found: {string.Join(", " , missingIds)}");

        var currentIds = problem.Tags.Select(x => x.Id).OrderBy(x => x).ToArray();
        var targetIds = tags.Select(x => x.Id).OrderBy(x => x).ToArray();

        if ( currentIds.SequenceEqual(targetIds) )
            return problem.ToDetailDto();

        try
        {
            problem.Tags.Clear();

            foreach ( var tag in tags.OrderBy(x => x.Name) )
                problem.Tags.Add(tag);

            await _unitOfWork.SaveChangesAsync(ct);
            return problem.ToDetailDto();
        }
        catch ( OperationCanceledException )
        {
            _logger.LogWarning(
                "Replace tags was cancelled. ProblemId={ProblemId}, TagIds={TagIds}" ,
                request.ProblemId ,
                normalizedTagIds);
            throw;
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(ex ,
                "Failed to replace tags for problem. ProblemId={ProblemId}, TagIds={TagIds}" ,
                request.ProblemId ,
                normalizedTagIds);

            throw new InvalidOperationException("Failed to replace problem tags.");
        }
    }
}