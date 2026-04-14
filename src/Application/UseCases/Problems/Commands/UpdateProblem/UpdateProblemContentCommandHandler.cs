using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Helpers;
using Application.UseCases.Problems.Mappings;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed class UpdateProblemContentCommandHandler : IRequestHandler<UpdateProblemContentCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2Service _r2Service;
    private readonly ILogger<UpdateProblemContentCommandHandler> _logger;

    public UpdateProblemContentCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IUnitOfWork unitOfWork ,
        IR2Service r2Service ,
        ILogger<UpdateProblemContentCommandHandler> logger)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _unitOfWork = unitOfWork;
        _r2Service = r2Service;
        _logger = logger;
    }

    public async Task<ProblemDetailDto> Handle(UpdateProblemContentCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var problem = await _problemRepository.GetProblemTrackedWithTagsAndTestsetsAsync(request.ProblemId , ct);
        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        var title = request.Title?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();

        ProblemCommandGuard.ValidateProblemCoreFields(
            title ,
            slug ,
            request.TimeLimitMs ,
            request.MemoryLimitKb);

        ProblemCommandGuard.ValidateStatementInput(request.DescriptionMd , request.StatementFile);

        var slugExists = await _problemRepository.SlugExistsAsync(slug! , request.ProblemId , ct);
        if ( slugExists )
            throw new InvalidOperationException($"Problem slug '{slug}' already exists.");

        var statusCode = ProblemCommandGuard.NormalizeStatusCode(request.StatusCode);
        var difficulty = ProblemCommandGuard.NormalizeDifficulty(request.Difficulty);
        var visibilityCode = ProblemCommandGuard.NormalizeVisibilityCode(request.VisibilityCode);
        var normalizedTagIds = ProblemCommandGuard.NormalizeTagIds(request.TagIds);

        var now = DateTime.UtcNow;

        problem.Title = title!;
        problem.Slug = slug!;
        problem.Difficulty = difficulty;
        problem.TypeCode = request.TypeCode?.Trim();
        problem.VisibilityCode = visibilityCode;
        problem.ScoringCode = request.ScoringCode?.Trim();
        problem.StatusCode = statusCode;
        problem.TimeLimitMs = request.TimeLimitMs;
        problem.MemoryLimitKb = request.MemoryLimitKb;
        problem.UpdatedAt = now;
        problem.UpdatedBy = _currentUser.UserId.Value;
        problem.PublishedAt = statusCode == "published"
            ? problem.PublishedAt ?? now
            : null;

        try
        {
            if ( request.StatementFile is not null && request.StatementFile.Length > 0 )
            {
                var (ext, contentType, sourceCode, _) =
                    ProblemCommandGuard.ResolveStatement(request.StatementFile);

                var normalizedStatementFileName = $"{slug}{ext}";

                if ( ext == ".md" )
                {
                    var markdownContent = await ProblemCommandGuard.ReadMarkdownFileAsync(request.StatementFile , ct);

                    problem.DescriptionMd = markdownContent;
                    problem.StatementSourceCode = "inline_md";
                    problem.StatementFileId = null;
                    problem.StatementFileName = normalizedStatementFileName;
                    problem.StatementContentType = contentType;
                    problem.StatementExtension = ext;
                    problem.StatementUploadedAt = now;
                }
                else
                {
                    var fileId = Guid.NewGuid();

                    await using var stream = request.StatementFile.OpenReadStream();
                    await _r2Service.UploadAsync(
                        type: "Problem" ,
                        id: fileId ,
                        fileExtension: ext ,
                        fileStream: stream ,
                        contentType: contentType ,
                        cancellationToken: ct);

                    problem.DescriptionMd = null;
                    problem.StatementSourceCode = sourceCode; // r2_pdf
                    problem.StatementFileId = fileId;
                    problem.StatementFileName = normalizedStatementFileName;
                    problem.StatementContentType = contentType;
                    problem.StatementExtension = ext;
                    problem.StatementUploadedAt = now;
                }
            }
            else
            {
                problem.DescriptionMd = request.DescriptionMd?.Trim();
                problem.StatementSourceCode = "inline_md";
                problem.StatementFileId = null;
                problem.StatementFileName = null;
                problem.StatementContentType = null;
                problem.StatementExtension = null;
                problem.StatementUploadedAt = null;
            }

            if ( request.TagIds is not null )
            {
                var tags = normalizedTagIds.Count == 0
                    ? []
                    : await _problemRepository.GetTagsTrackedByIdsAsync(normalizedTagIds , ct);

                var foundIds = tags.Select(x => x.Id).ToHashSet();
                var missingIds = normalizedTagIds.Where(x => !foundIds.Contains(x)).ToArray();

                if ( missingIds.Length > 0 )
                    throw new KeyNotFoundException($"Some tags were not found: {string.Join(", " , missingIds)}");

                problem.Tags.Clear();

                foreach ( var tag in tags.OrderBy(x => x.Name) )
                    problem.Tags.Add(tag);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return problem.ToDetailDto();
        }
        catch ( OperationCanceledException )
        {
            _logger.LogWarning("Update problem was cancelled. ProblemId={ProblemId}" , request.ProblemId);
            throw;
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(ex , "Database update failed while updating problem. ProblemId={ProblemId}" , request.ProblemId);
            throw new InvalidOperationException("Failed to update problem. Please verify input data and try again.");
        }
        catch ( IOException ex )
        {
            _logger.LogError(ex , "I/O error while handling statement for problem. ProblemId={ProblemId}" , request.ProblemId);
            throw new InvalidOperationException("Failed to process statement file. Please try again.");
        }
        catch ( Exception ex ) when ( ex is not ArgumentException and not InvalidOperationException and not KeyNotFoundException and not UnauthorizedAccessException )
        {
            _logger.LogError(ex , "Unexpected error while updating problem. ProblemId={ProblemId}" , request.ProblemId);
            throw new InvalidOperationException("Unexpected error occurred while updating problem.");
        }
    }
}