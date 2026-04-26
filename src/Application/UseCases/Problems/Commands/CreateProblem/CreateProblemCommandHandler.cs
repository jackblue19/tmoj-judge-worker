using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Helpers;
using Application.UseCases.Problems.Mappings;
using Domain.Abstractions;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed class CreateProblemCommandHandler : IRequestHandler<CreateProblemCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IWriteRepository<Problem , Guid> _problemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2Service _r2Service;
    private readonly ILogger<CreateProblemCommandHandler> _logger;

    public CreateProblemCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IWriteRepository<Problem , Guid> problemWriteRepository ,
        IUnitOfWork unitOfWork ,
        IR2Service r2Service ,
        ILogger<CreateProblemCommandHandler> logger)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _problemWriteRepository = problemWriteRepository;
        _unitOfWork = unitOfWork;
        _r2Service = r2Service;
        _logger = logger;
    }

    public async Task<ProblemDetailDto> Handle(CreateProblemCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var title = request.Title?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();

        ProblemCommandGuard.ValidateProblemCoreFields(
            title ,
            slug ,
            request.TimeLimitMs ,
            request.MemoryLimitKb);

        ProblemCommandGuard.ValidateStatementInput(request.DescriptionMd , request.StatementFile);

        var difficulty = ProblemCommandGuard.NormalizeDifficulty(request.Difficulty);
        var normalizedTagIds = ProblemCommandGuard.NormalizeTagIds(request.TagIds);

        // hardcode backend
        const string visibilityCode = "public";
        const string statusCode = "published";

        var problemMode = NormalizeProblemMode(request.ProblemMode);
        var problemSource = ProblemSourceCodes.Origin;

        var slugExists = await _problemRepository.SlugExistsAsync(slug! , null , ct);
        if ( slugExists )
            throw new InvalidOperationException($"Problem slug '{slug}' already exists.");

        var now = DateTime.UtcNow;

        var entity = new Problem
        {
            Id = Guid.NewGuid() ,
            Title = title! ,
            Slug = slug! ,
            Difficulty = difficulty ,
            StatusCode = statusCode ,
            TypeCode = request.TypeCode?.Trim() ,
            VisibilityCode = visibilityCode ,
            ScoringCode = request.ScoringCode?.Trim() ,
            TimeLimitMs = request.TimeLimitMs ,
            MemoryLimitKb = request.MemoryLimitKb ,

            // new fields
            ProblemMode = problemMode ,
            ProblemSource = problemSource ,
            UsedCount = 0 ,
            OriginId = null ,

            PublishedAt = now ,
            IsActive = true ,
            CreatedAt = now ,
            CreatedBy = _currentUser.UserId.Value ,
            UpdatedAt = now ,
            UpdatedBy = _currentUser.UserId.Value
        };

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

                    entity.DescriptionMd = markdownContent;
                    entity.StatementSourceCode = "inline_md";
                    entity.StatementFileId = null;
                    entity.StatementFileName = normalizedStatementFileName;
                    entity.StatementContentType = contentType;
                    entity.StatementExtension = ext;
                    entity.StatementUploadedAt = now;
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

                    entity.DescriptionMd = null;
                    entity.StatementSourceCode = sourceCode;
                    entity.StatementFileId = fileId;
                    entity.StatementFileName = normalizedStatementFileName;
                    entity.StatementContentType = contentType;
                    entity.StatementExtension = ext;
                    entity.StatementUploadedAt = now;
                }
            }
            else
            {
                entity.DescriptionMd = request.DescriptionMd?.Trim();
                entity.StatementSourceCode = "inline_md";
                entity.StatementFileId = null;
                entity.StatementFileName = null;
                entity.StatementContentType = null;
                entity.StatementExtension = null;
                entity.StatementUploadedAt = null;
            }

            if ( normalizedTagIds.Count > 0 )
            {
                var tags = await _problemRepository.GetTagsTrackedByIdsAsync(normalizedTagIds , ct);

                var foundIds = tags.Select(x => x.Id).ToHashSet();
                var missingIds = normalizedTagIds.Where(x => !foundIds.Contains(x)).ToArray();

                if ( missingIds.Length > 0 )
                    throw new KeyNotFoundException($"Some tags were not found: {string.Join(", " , missingIds)}");

                foreach ( var tag in tags.OrderBy(x => x.Name) )
                    entity.Tags.Add(tag);
            }

            await _problemWriteRepository.AddAsync(entity , ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return entity.ToDetailDto();
        }
        catch ( OperationCanceledException )
        {
            _logger.LogWarning("Create problem was cancelled. ProblemId={ProblemId}" , entity.Id);
            throw;
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(ex , "Database update failed while creating problem. Slug={Slug}" , slug);
            throw new InvalidOperationException("Failed to save problem. Please verify input data and try again.");
        }
        catch ( IOException ex )
        {
            _logger.LogError(ex , "I/O error while handling statement for problem. Slug={Slug}" , slug);
            throw new InvalidOperationException("Failed to process statement file. Please try again.");
        }
        catch ( Exception ex ) when ( ex is not ArgumentException and not InvalidOperationException and not KeyNotFoundException and not UnauthorizedAccessException )
        {
            _logger.LogError(ex , "Unexpected error while creating problem. Slug={Slug}" , slug);
            throw new InvalidOperationException("Unexpected error occurred while creating problem.");
        }
    }

    private static string NormalizeProblemMode(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        if ( string.IsNullOrWhiteSpace(normalized) )
            return ProblemModeCodes.Pro;

        return normalized switch
        {
            ProblemModeCodes.Amateur => ProblemModeCodes.Amateur,
            ProblemModeCodes.Pro => ProblemModeCodes.Pro,
            _ => throw new ArgumentException("Problem mode must be 'amateur' or 'pro'.")
        };
    }
}