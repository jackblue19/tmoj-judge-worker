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

namespace Application.UseCases.Problems.Commands.CreateVirtualProblem;

public sealed class CreateVirtualProblemCommandHandler
    : IRequestHandler<CreateVirtualProblemCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IWriteRepository<Problem , Guid> _problemWriteRepository;
    private readonly IWriteRepository<ProblemTemplate , Guid> _problemTemplateWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateVirtualProblemCommandHandler> _logger;

    public CreateVirtualProblemCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IReadRepository<Problem , Guid> problemReadRepository ,
        IWriteRepository<Problem , Guid> problemWriteRepository ,
        IWriteRepository<ProblemTemplate , Guid> problemTemplateWriteRepository ,
        IUnitOfWork unitOfWork ,
        ILogger<CreateVirtualProblemCommandHandler> logger)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _problemReadRepository = problemReadRepository;
        _problemWriteRepository = problemWriteRepository;
        _problemTemplateWriteRepository = problemTemplateWriteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProblemDetailDto> Handle(CreateVirtualProblemCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var requestedOriginSlug = request.OriginProblemSlug?.Trim().ToLowerInvariant();
        var titleOverride = request.Title?.Trim();

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

        var slug = await ProblemSlugHelper.ResolveVirtualSlugAsync(
            request.Slug ,
            origin.Slug ,
            _problemRepository ,
            ct);

        var visibilityCode = ProblemCommandGuard.NormalizeVisibilityForVirtual(request.VisibilityCode);

        var now = DateTime.UtcNow;

        var rootOriginId = origin.ProblemSource == ProblemSourceCodes.Origin
                                                ? origin.Id
                                                : origin.OriginId ?? origin.Id;

        var entity = new Problem
        {
            Id = Guid.NewGuid() ,
            Title = string.IsNullOrWhiteSpace(titleOverride) ? origin.Title : titleOverride ,
            Slug = slug ,
            Difficulty = origin.Difficulty ,
            TypeCode = origin.TypeCode ,
            VisibilityCode = visibilityCode ,
            ScoringCode = origin.ScoringCode ,
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

            DescriptionMd = origin.DescriptionMd ,
            AcceptancePercent = origin.AcceptancePercent ,
            DisplayIndex = origin.DisplayIndex ,
            TimeLimitMs = origin.TimeLimitMs ,
            MemoryLimitKb = origin.MemoryLimitKb ,

            ProblemMode = origin.ProblemMode ,
            ProblemSource = ProblemSourceCodes.Virtual ,
            UsedCount = 0 ,
            OriginId = rootOriginId ,

            StatementSourceCode = origin.StatementSourceCode ,
            StatementFileId = origin.StatementFileId ,
            StatementFileName = origin.StatementFileName ,
            StatementContentType = origin.StatementContentType ,
            StatementExtension = origin.StatementExtension ,
            StatementUploadedAt = origin.StatementUploadedAt
        };

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
                WrapperType = x.WrapperType ,
                Version = x.Version ,
                IsActive = x.IsActive ,
                CreatedAt = now ,
                CreatedBy = _currentUser.UserId.Value
            })
            .ToList();

        try
        {
            // 1. save problem first
            await _problemWriteRepository.AddAsync(entity , ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 2. attach tags by tracked instances from DB, then save relation
            var originTagIds = origin.Tags
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            if ( originTagIds.Count > 0 )
            {
                var trackedTags = await _problemRepository.GetTagsTrackedByIdsAsync(originTagIds , ct);

                foreach ( var tag in trackedTags.OrderBy(x => x.Name) )
                    entity.Tags.Add(tag);

                await _unitOfWork.SaveChangesAsync(ct);
            }

            // 3. save templates last
            if ( clonedTemplates.Count > 0 )
            {
                await _problemTemplateWriteRepository.AddRangeAsync(clonedTemplates , ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return entity.ToDetailDto();
        }
        catch ( OperationCanceledException )
        {
            _logger.LogWarning(
                "Create virtual problem was cancelled. OriginProblemId={OriginProblemId}, NewProblemId={ProblemId}" ,
                origin.Id ,
                entity.Id);
            throw;
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(
                ex ,
                "Database update failed while creating virtual problem. OriginProblemId={OriginProblemId}, Slug={Slug}" ,
                origin.Id ,
                slug);

            throw new InvalidOperationException("Failed to create virtual problem. Please verify input data and try again.");
        }
        catch ( Exception ex ) when ( ex is not ArgumentException and not InvalidOperationException and not KeyNotFoundException and not UnauthorizedAccessException )
        {
            _logger.LogError(
                ex ,
                "Unexpected error while creating virtual problem. OriginProblemId={OriginProblemId}, Slug={Slug}" ,
                origin.Id ,
                slug);

            throw new InvalidOperationException("Unexpected error occurred while creating virtual problem.");
        }
    }
}