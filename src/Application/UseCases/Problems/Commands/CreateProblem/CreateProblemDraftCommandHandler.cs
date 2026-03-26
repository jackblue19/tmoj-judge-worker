using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed class CreateProblemDraftCommandHandler : IRequestHandler<CreateProblemDraftCommand , ProblemSummaryDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IWriteRepository<Problem , Guid> _problemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2Service _r2Service;

    public CreateProblemDraftCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IWriteRepository<Problem , Guid> problemWriteRepository ,
        IUnitOfWork unitOfWork ,
        IR2Service r2Service)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
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

    public async Task<ProblemSummaryDto> Handle(CreateProblemDraftCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var title = request.Title?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();

        if ( string.IsNullOrWhiteSpace(title) )
            throw new ArgumentException("Title is required.");

        if ( string.IsNullOrWhiteSpace(slug) )
            throw new ArgumentException("Slug is required.");

        ValidateStatementInput(request.DescriptionMd , request.StatementFile);

        var slugExists = await _problemRepository.SlugExistsAsync(slug , null , ct);
        if ( slugExists )
            throw new InvalidOperationException($"Problem slug '{slug}' already exists.");

        if ( request.StatementFile is null && string.IsNullOrWhiteSpace(request.DescriptionMd) )
            throw new ArgumentException("Either description markdown or statement file is required.");

        var now = DateTime.UtcNow;

        var entity = new Problem
        {
            Id = Guid.NewGuid() ,
            Title = title ,
            Slug = slug ,
            StatusCode = ProblemStatusCodes.Draft ,
            TypeCode = request.TypeCode?.Trim() ,
            ScoringCode = request.ScoringCode?.Trim() ,
            VisibilityCode = request.VisibilityCode?.Trim() ,
            TimeLimitMs = request.TimeLimitMs ,
            MemoryLimitKb = request.MemoryLimitKb ,
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
            entity.StatementFileName = Path.GetFileName(request.StatementFile.FileName);
            entity.StatementContentType = contentType;
            entity.StatementExtension = ext;
            entity.StatementUploadedAt = now;
        }
        else
        {
            entity.DescriptionMd = request.DescriptionMd?.Trim();
            entity.StatementSourceCode = ProblemStatementSourceCodes.InlineMd;
            entity.StatementFileId = null;
            entity.StatementFileName = null;
            entity.StatementContentType = null;
            entity.StatementExtension = null;
            entity.StatementUploadedAt = null;
        }

        await _problemWriteRepository.AddAsync(entity , ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ProblemSummaryDto
        {
            Id = entity.Id ,
            Title = entity.Title ,
            Slug = entity.Slug ,
            StatusCode = entity.StatusCode ,
            Difficulty = entity.Difficulty ,
            TimeLimitMs = entity.TimeLimitMs ,
            MemoryLimitKb = entity.MemoryLimitKb ,
            IsActive = entity.IsActive ,
            CreatedAt = entity.CreatedAt ,
            UpdatedAt = entity.UpdatedAt
        };
    }
}