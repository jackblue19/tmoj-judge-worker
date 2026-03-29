using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Testsets.Dtos;
using Application.UseCases.Testsets.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System.IO.Compression;

namespace Application.UseCases.Testsets.Queries;

public sealed class DownloadTestsetZipQueryHandler
    : IRequestHandler<DownloadTestsetZipQuery , DownloadTestsetZipDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IReadRepository<Testset , Guid> _testsetReadRepository;
    private readonly IReadRepository<Testcase , Guid> _testcaseReadRepository;
    private readonly IR2Service _r2Service;

    public DownloadTestsetZipQueryHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Problem , Guid> problemReadRepository ,
        IReadRepository<Testset , Guid> testsetReadRepository ,
        IReadRepository<Testcase , Guid> testcaseReadRepository ,
        IR2Service r2Service)
    {
        _currentUser = currentUser;
        _problemReadRepository = problemReadRepository;
        _testsetReadRepository = testsetReadRepository;
        _testcaseReadRepository = testcaseReadRepository;
        _r2Service = r2Service;
    }

    public async Task<DownloadTestsetZipDto> Handle(
        DownloadTestsetZipQuery request ,
        CancellationToken ct)
    {
        EnsureAuthenticated();

        var problem = await _problemReadRepository.GetByIdAsync(request.ProblemId , ct);
        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        EnsureCanManageProblem(problem);

        var testset = await _testsetReadRepository.GetByIdAsync(request.TestsetId , ct);
        if ( testset is null || testset.ProblemId != request.ProblemId )
            throw new KeyNotFoundException("Testset not found.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            throw new InvalidOperationException("Problem slug is required.");

        var testcases = await _testcaseReadRepository.ListAsync(
            new TestcasesByTestsetSpec(request.TestsetId) , ct);

        if ( testcases.Count == 0 )
            throw new InvalidOperationException("No testcase metadata found for this testset.");

        var zipFileName = $"{problem.Slug}.zip";

        await using var memoryStream = new MemoryStream();

        using ( var archive = new ZipArchive(memoryStream , ZipArchiveMode.Create , leaveOpen: true) )
        {
            foreach ( var testcase in testcases )
            {
                var ordinalFolder = $"{problem.Slug}/{testcase.Ordinal:D3}/";

                var inputKey = await ResolveExistingObjectKeyAsync(
                    request.TestsetId ,
                    testcase.Ordinal ,
                    isInput: true ,
                    ct);

                var outputKey = await ResolveExistingObjectKeyAsync(
                    request.TestsetId ,
                    testcase.Ordinal ,
                    isInput: false ,
                    ct);

                var inputText = await _r2Service.GetObjectTextAsync("Testset" , inputKey , ct);
                var outputText = await _r2Service.GetObjectTextAsync("Testset" , outputKey , ct);

                var inputEntry = archive.CreateEntry(
                    $"{ordinalFolder}{problem.Slug}.inp" ,
                    CompressionLevel.Fastest);

                await using ( var inputEntryStream = inputEntry.Open() )
                await using ( var inputWriter = new StreamWriter(inputEntryStream) )
                {
                    await inputWriter.WriteAsync(inputText);
                }

                var outputEntry = archive.CreateEntry(
                    $"{ordinalFolder}{problem.Slug}.out" ,
                    CompressionLevel.Fastest);

                await using ( var outputEntryStream = outputEntry.Open() )
                await using ( var outputWriter = new StreamWriter(outputEntryStream) )
                {
                    await outputWriter.WriteAsync(outputText);
                }
            }
        }

        return new DownloadTestsetZipDto
        {
            FileName = zipFileName ,
            ContentType = "application/zip" ,
            Bytes = memoryStream.ToArray()
        };
    }

    private async Task<string> ResolveExistingObjectKeyAsync(
        Guid testsetId ,
        int ordinal ,
        bool isInput ,
        CancellationToken ct)
    {
        var baseFolder = $"{testsetId:D}/{ordinal:D3}/";

        var candidates = isInput
            ? new[]
            {
                $"{baseFolder}input.inp",
                $"{baseFolder}input.txt"
            }
            : new[]
            {
                $"{baseFolder}output.out",
                $"{baseFolder}output.txt"
            };

        var existingKeys = await _r2Service.ListObjectKeysAsync("Testset" , baseFolder , ct);

        foreach ( var candidate in candidates )
        {
            if ( existingKeys.Contains(candidate , StringComparer.OrdinalIgnoreCase) )
                return candidate;
        }

        throw new FileNotFoundException(
            $"Cannot find {(isInput ? "input" : "output")} object for testcase ordinal {ordinal}.");
    }

    private void EnsureAuthenticated()
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");
    }

    private void EnsureCanManageProblem(Problem problem)
    {
        var isAdmin = _currentUser.IsInRole("Admin");
        if ( isAdmin ) return;

        var currentUserId = _currentUser.UserId!.Value;

        if ( problem.CreatedBy != currentUserId )
            throw new KeyNotFoundException("Problem not found or access denied.");
    }
}