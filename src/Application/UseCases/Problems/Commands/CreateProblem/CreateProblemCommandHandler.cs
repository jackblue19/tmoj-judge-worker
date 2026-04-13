using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Mappings;
using Application.UseCases.Problems.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed class CreateProblemCommandHandler : IRequestHandler<CreateProblemCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IReadRepository<Tag , Guid> _tagReadRepository;
    private readonly IWriteRepository<Problem , Guid> _problemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2Service _r2Service;

    public CreateProblemCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IReadRepository<Tag , Guid> tagReadRepository ,
        IWriteRepository<Problem , Guid> problemWriteRepository ,
        IUnitOfWork unitOfWork ,
        IR2Service r2Service)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _tagReadRepository = tagReadRepository;
        _problemWriteRepository = problemWriteRepository;
        _unitOfWork = unitOfWork;
        _r2Service = r2Service;
    }

    private static void ValidateStatementInput(string? descriptionMd , IFormFile? statementFile)
    {
        var hasDescription = !string.IsNullOrWhiteSpace(descriptionMd);
        var hasFile = statementFile is not null && statementFile.Length > 0;

        if ( !hasDescription && !hasFile )
            throw new ArgumentException("Either description markdown or statement file is required.");

        if ( hasDescription && hasFile )
            throw new ArgumentException("Description markdown and statement file cannot be provided together.");
    }

    private static (string ext, string contentType, string sourceCode) ResolveStatement(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        return ext switch
        {
            ".md" => (".md", "text/markdown; charset=utf-8", ProblemStatementSourceCodes.R2Md),
            ".pdf" => (".pdf", "application/pdf", ProblemStatementSourceCodes.R2Pdf),
            _ => throw new ArgumentException("Statement file must be .md or .pdf.")
        };
    }

    private static string NormalizeStatusCode(string? statusCode)
    {
        var normalized = statusCode?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" => ProblemStatusCodes.Draft,
            "draft" => "draft",
            "pending" => "pending",
            "published" => "published",
            "archived" => "archived",
            _ => throw new ArgumentException("Invalid status code.")
        };
    }

    public async Task<ProblemDetailDto> Handle(CreateProblemCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var title = request.Title?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();
        var statementFileName = slug + ".md";

        if ( string.IsNullOrWhiteSpace(title) )
            throw new ArgumentException("Title is required.");

        if ( string.IsNullOrWhiteSpace(slug) )
            throw new ArgumentException("Slug is required.");

        ValidateStatementInput(request.DescriptionMd , request.StatementFile);

        var slugExists = await _problemRepository.SlugExistsAsync(slug , null , ct);
        if ( slugExists )
            throw new InvalidOperationException($"Problem slug '{slug}' already exists.");

        var now = DateTime.UtcNow;
        var statusCode = NormalizeStatusCode(request.StatusCode);

        var entity = new Problem
        {
            Id = Guid.NewGuid() ,
            Title = title ,
            Slug = slug ,
            StatementFileName = statementFileName ,
            Difficulty = request.Difficulty?.Trim() ,
            StatusCode = statusCode ,
            TypeCode = request.TypeCode?.Trim() ,
            VisibilityCode = request.VisibilityCode?.Trim() ,
            ScoringCode = request.ScoringCode?.Trim() ,
            TimeLimitMs = request.TimeLimitMs ,
            MemoryLimitKb = request.MemoryLimitKb ,
            PublishedAt = statusCode == "published" ? now : null ,
            IsActive = true ,
            CreatedAt = now ,
            CreatedBy = _currentUser.UserId.Value ,
            UpdatedAt = now ,
            UpdatedBy = _currentUser.UserId.Value
        };

        if ( request.StatementFile is not null && request.StatementFile.Length > 0 )
        {
            var (ext, contentType, sourceCode) = ResolveStatement(request.StatementFile);
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
            //entity.StatementFileName = Path.GetFileName(request.StatementFile.FileName);
            entity.StatementFileName = Path.GetFileName(statementFileName);
            entity.StatementContentType = contentType;
            entity.StatementExtension = ext;
            entity.StatementUploadedAt = now;
        }
        else
        {
            entity.DescriptionMd = request.DescriptionMd?.Trim();
            entity.StatementSourceCode = ProblemStatementSourceCodes.InlineMd;
            entity.StatementFileId = null;
            //entity.StatementFileName = null;
            entity.StatementContentType = null;
            entity.StatementExtension = null;
            entity.StatementUploadedAt = null;
        }

        if ( request.TagIds is not null && request.TagIds.Count > 0 )
        {
            var tags = await _tagReadRepository.ListAsync(new TagsByIdsSpec(request.TagIds) , ct);

            var foundIds = tags.Select(x => x.Id).ToHashSet();
            var missingIds = request.TagIds.Where(x => !foundIds.Contains(x)).ToArray();

            if ( missingIds.Length > 0 )
                throw new KeyNotFoundException($"Some tags were not found: {string.Join(", " , missingIds)}");

            foreach ( var tag in tags )
                entity.Tags.Add(tag);
        }

        await _problemWriteRepository.AddAsync(entity , ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDetailDto();
    }
}