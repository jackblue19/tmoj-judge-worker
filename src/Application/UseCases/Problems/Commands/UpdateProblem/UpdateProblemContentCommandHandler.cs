using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed class UpdateProblemContentCommandHandler : IRequestHandler<UpdateProblemContentCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2Service _r2Service;

    public UpdateProblemContentCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IUnitOfWork unitOfWork ,
        IR2Service r2Service)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
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

    public async Task<ProblemDetailDto> Handle(UpdateProblemContentCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        var entity = await _problemRepository.GetProblemForManagementAsync(
            request.ProblemId ,
            currentUserId ,
            isAdmin ,
            ct);

        if ( entity is null )
            throw new KeyNotFoundException("Problem not found or access denied.");

        var title = request.Title?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();

        if ( string.IsNullOrWhiteSpace(title) )
            throw new ArgumentException("Title is required.");

        if ( string.IsNullOrWhiteSpace(slug) )
            throw new ArgumentException("Slug is required.");

        ValidateStatementInput(request.DescriptionMd , request.StatementFile);

        var slugExists = await _problemRepository.SlugExistsAsync(slug , entity.Id , ct);
        if ( slugExists )
            throw new InvalidOperationException($"Problem slug '{slug}' already exists.");

        entity.Title = title;
        entity.Slug = slug;
        entity.TimeLimitMs = request.TimeLimitMs;
        entity.MemoryLimitKb = request.MemoryLimitKb;
        entity.TypeCode = request.TypeCode?.Trim();
        entity.ScoringCode = request.ScoringCode?.Trim();
        entity.VisibilityCode = request.VisibilityCode?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUserId;

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
            entity.StatementUploadedAt = DateTime.UtcNow;
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

        await _unitOfWork.SaveChangesAsync(ct);

        var detail = await _problemRepository.GetProblemDetailForManagementAsync(
            entity.Id ,
            currentUserId ,
            isAdmin ,
            ct);

        return detail ?? throw new KeyNotFoundException("Problem detail not found after update.");
    }
}