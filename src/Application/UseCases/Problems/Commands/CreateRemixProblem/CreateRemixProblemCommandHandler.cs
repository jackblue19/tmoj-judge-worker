using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Helpers;
using Application.UseCases.Problems.Mappings;
using Application.UseCases.Problems.Specifications;
using Domain.Abstractions;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Problems.Commands.CreateRemixProblem;

public sealed class CreateRemixProblemCommandHandler
    : IRequestHandler<CreateRemixProblemCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IWriteRepository<Problem , Guid> _problemWriteRepository;
    private readonly IWriteRepository<ProblemTemplate , Guid> _problemTemplateWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2Service _r2Service;
    private readonly ILogger<CreateRemixProblemCommandHandler> _logger;

    public CreateRemixProblemCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IReadRepository<Problem , Guid> problemReadRepository ,
        IWriteRepository<Problem , Guid> problemWriteRepository ,
        IWriteRepository<ProblemTemplate , Guid> problemTemplateWriteRepository ,
        IUnitOfWork unitOfWork ,
        IR2Service r2Service ,
        ILogger<CreateRemixProblemCommandHandler> logger)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _problemReadRepository = problemReadRepository;
        _problemWriteRepository = problemWriteRepository;
        _problemTemplateWriteRepository = problemTemplateWriteRepository;
        _unitOfWork = unitOfWork;
        _r2Service = r2Service;
        _logger = logger;
    }

    public async Task<ProblemDetailDto> Handle(CreateRemixProblemCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var requestedOriginSlug = request.OriginProblemSlug?.Trim().ToLowerInvariant();

        if ( request.OriginProblemId is null && string.IsNullOrWhiteSpace(requestedOriginSlug) )
            throw new ArgumentException("Either OriginProblemId or OriginProblemSlug is required.");

        Problem? origin;

        if ( request.OriginProblemId.HasValue && request.OriginProblemId.Value != Guid.Empty )
        {
            origin = await _problemReadRepository.FirstOrDefaultAsync(
                new ProblemCloneSourceByIdSpec(request.OriginProblemId.Value) ,
                ct);
        }
        else
        {
            origin = await _problemReadRepository.FirstOrDefaultAsync(
                new ProblemCloneSourceBySlugSpec(requestedOriginSlug!) ,
                ct);
        }

        if ( origin is null )
            throw new KeyNotFoundException("Origin problem not found.");

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? origin.Title
            : request.Title.Trim();

        var slug = await ProblemSlugHelper.ResolveRemixSlugAsync(
            request.Slug ,
            origin.Slug ,
            _problemRepository ,
            ct);

        var difficulty = request.Difficulty is null
            ? origin.Difficulty
            : ProblemCommandGuard.NormalizeDifficulty(request.Difficulty);

        var problemMode = request.ProblemMode is null
            ? origin.ProblemMode
            : ProblemCommandGuard.NormalizeProblemMode(request.ProblemMode);

        var visibilityCode = ProblemCommandGuard.NormalizeVisibilityForClone(
            request.VisibilityCode ,
            origin.VisibilityCode);

        var typeCode = string.IsNullOrWhiteSpace(request.TypeCode)
            ? origin.TypeCode
            : request.TypeCode.Trim();

        var scoringCode = string.IsNullOrWhiteSpace(request.ScoringCode)
            ? origin.ScoringCode
            : request.ScoringCode.Trim();

        var timeLimitMs = request.TimeLimitMs ?? origin.TimeLimitMs;
        var memoryLimitKb = request.MemoryLimitKb ?? origin.MemoryLimitKb;

        ProblemCommandGuard.ValidateProblemCoreFields(
            title ,
            slug ,
            timeLimitMs ,
            memoryLimitKb);

        if ( request.StatementFile is not null && !string.IsNullOrWhiteSpace(request.DescriptionMd) )
            throw new ArgumentException("Description markdown and statement file cannot be provided together.");

        var now = DateTime.UtcNow;

        var rootOriginId = origin.ProblemSource == ProblemSourceCodes.Origin
            ? origin.Id
            : origin.OriginId ?? origin.Id;

        var entity = new Problem
        {
            Id = Guid.NewGuid() ,
            Title = title! ,
            Slug = slug ,
            Difficulty = difficulty ,
            TypeCode = typeCode ,
            VisibilityCode = visibilityCode ,
            ScoringCode = scoringCode ,
            StatusCode = origin.StatusCode ,

            ApprovedByUserId = origin.ApprovedByUserId ,
            ApprovedAt = origin.ApprovedAt ,
            PublishedAt = origin.StatusCode == ProblemStatusCodes.Published
                ? origin.PublishedAt ?? now
                : null ,

            IsActive = true ,
            CreatedAt = now ,
            CreatedBy = _currentUser.UserId.Value ,
            UpdatedAt = now ,
            UpdatedBy = _currentUser.UserId.Value ,

            AcceptancePercent = origin.AcceptancePercent ,
            DisplayIndex = origin.DisplayIndex ,
            TimeLimitMs = timeLimitMs ,
            MemoryLimitKb = memoryLimitKb ,

            ProblemMode = problemMode ,
            ProblemSource = ProblemSourceCodes.Remix ,
            UsedCount = 0 ,
            OriginId = rootOriginId
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
                    entity.StatementSourceCode = StatementSourceCodes.InlineMarkdown;
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
            else if ( request.DescriptionMd is not null )
            {
                entity.DescriptionMd = request.DescriptionMd.Trim();
                entity.StatementSourceCode = StatementSourceCodes.InlineMarkdown;
                entity.StatementFileId = null;
                entity.StatementFileName = null;
                entity.StatementContentType = null;
                entity.StatementExtension = null;
                entity.StatementUploadedAt = null;
            }
            else
            {
                entity.DescriptionMd = origin.DescriptionMd;
                entity.StatementSourceCode = origin.StatementSourceCode;
                entity.StatementFileId = origin.StatementFileId;
                entity.StatementFileName = origin.StatementFileName;
                entity.StatementContentType = origin.StatementContentType;
                entity.StatementExtension = origin.StatementExtension;
                entity.StatementUploadedAt = origin.StatementUploadedAt;
            }

            if ( request.TagIds is null )
            {
                foreach ( var tag in origin.Tags.OrderBy(x => x.Name) )
                    entity.Tags.Add(tag);
            }
            else
            {
                var normalizedTagIds = ProblemCommandGuard.NormalizeTagIds(request.TagIds);

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
            }

            var clonedTemplates = origin.ProblemTemplates
                .Where(x => x.IsActive)
                .OrderBy(x => x.RuntimeId)
                .ThenByDescending(x => x.Version)
                .Select(x => new ProblemTemplate
                {
                    CodeTemplateId = Guid.NewGuid() ,
                    ProblemId = entity.Id ,
                    RuntimeId = x.RuntimeId ,
                    TemplateCode = x.TemplateCode ,
                    InjectionPoint = x.InjectionPoint ,
                    SolutionSignature = x.SolutionSignature ,

                    // ép cứng theo problem mode mới của remix
                    WrapperType = problemMode == ProblemModeCodes.Amateur ? "function_only" : "full" ,

                    Version = x.Version ,
                    IsActive = x.IsActive ,
                    CreatedAt = now ,
                    CreatedBy = _currentUser.UserId.Value
                })
                .ToList();

            await _problemWriteRepository.AddAsync(entity , ct);

            if ( clonedTemplates.Count > 0 )
                await _problemTemplateWriteRepository.AddRangeAsync(clonedTemplates , ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return entity.ToDetailDto();
        }
        catch ( OperationCanceledException )
        {
            _logger.LogWarning(
                "Create remix problem was cancelled. OriginProblemId={OriginProblemId}, NewProblemId={ProblemId}" ,
                origin.Id ,
                entity.Id);
            throw;
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(
                ex ,
                "Database update failed while creating remix problem. OriginProblemId={OriginProblemId}, Slug={Slug}" ,
                origin.Id ,
                slug);

            throw new InvalidOperationException("Failed to create remix problem. Please verify input data and try again.");
        }
        catch ( IOException ex )
        {
            _logger.LogError(
                ex ,
                "I/O error while handling statement for remix problem. OriginProblemId={OriginProblemId}, Slug={Slug}" ,
                origin.Id ,
                slug);

            throw new InvalidOperationException("Failed to process statement file. Please try again.");
        }
        catch ( Exception ex ) when ( ex is not ArgumentException and not InvalidOperationException and not KeyNotFoundException and not UnauthorizedAccessException )
        {
            _logger.LogError(
                ex ,
                "Unexpected error while creating remix problem. OriginProblemId={OriginProblemId}, Slug={Slug}" ,
                origin.Id ,
                slug);

            throw new InvalidOperationException("Unexpected error occurred while creating remix problem.");
        }
    }
}