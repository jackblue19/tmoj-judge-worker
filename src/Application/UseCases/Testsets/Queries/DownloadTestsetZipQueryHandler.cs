using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Testsets.Dtos;
using Application.UseCases.Testsets.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Text;

namespace Application.UseCases.Testsets.Queries;

public sealed class DownloadTestsetZipQueryHandler
    : IRequestHandler<DownloadTestsetZipQuery , DownloadTestsetZipDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IReadRepository<Testset , Guid> _testsetReadRepository;
    private readonly IReadRepository<Testcase , Guid> _testcaseReadRepository;
    private readonly IR2Service _r2Service;
    private readonly int _maxParallelDownloads;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DownloadTestsetZipQueryHandler(
     ICurrentUserService currentUser ,
     IReadRepository<Problem , Guid> problemReadRepository ,
     IReadRepository<Testset , Guid> testsetReadRepository ,
     IReadRepository<Testcase , Guid> testcaseReadRepository ,
     IR2Service r2Service ,
     IConfiguration configuration ,
     IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = currentUser;
        _problemReadRepository = problemReadRepository;
        _testsetReadRepository = testsetReadRepository;
        _testcaseReadRepository = testcaseReadRepository;
        _r2Service = r2Service;
        _httpContextAccessor = httpContextAccessor;
        _maxParallelDownloads = configuration.GetValue<int?>("TestsetSettings:MaxParallelDownloads") ?? 4;
    }

    public async Task<DownloadTestsetZipDto> Handle(
        DownloadTestsetZipQuery request ,
        CancellationToken ct)
    {
        var problem = await _problemReadRepository.GetByIdAsync(request.ProblemId , ct);
        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        var isInternal = _httpContextAccessor.HttpContext?.Items["IsInternal"] as bool? ?? false;

        if ( !isInternal )
        {
            EnsureAuthenticated();
            EnsureCanManageProblem(problem);
        }

        var testset = await _testsetReadRepository.GetByIdAsync(request.TestsetId , ct);
        if ( testset is null || testset.ProblemId != request.ProblemId )
            throw new KeyNotFoundException("Testset not found.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            throw new InvalidOperationException("Problem slug is required.");

        var testcases = await _testcaseReadRepository.ListAsync(
            new TestcasesByTestsetSpec(request.TestsetId) , ct);

        if ( testcases.Count == 0 )
            throw new InvalidOperationException("No testcase metadata found for this testset.");

        var semaphore = new SemaphoreSlim(_maxParallelDownloads);

        try
        {
            // Parallel fetch from R2 (bounded)
            var fetchTasks = testcases.Select(async testcase =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    if ( string.IsNullOrWhiteSpace(testcase.Input) )
                        throw new InvalidOperationException(
                            $"Missing input object key for testcase ordinal {testcase.Ordinal}.");

                    if ( string.IsNullOrWhiteSpace(testcase.ExpectedOutput) )
                        throw new InvalidOperationException(
                            $"Missing output object key for testcase ordinal {testcase.Ordinal}.");

                    var inputTask = _r2Service.GetObjectTextAsync("Testset" , testcase.Input , ct);
                    var outputTask = _r2Service.GetObjectTextAsync("Testset" , testcase.ExpectedOutput , ct);

                    await Task.WhenAll(inputTask , outputTask);

                    return new DownloadZipFetchedTestcase
                    {
                        Ordinal = testcase.Ordinal ,
                        InputText = await inputTask ,
                        OutputText = await outputTask
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var fetchedItems = (await Task.WhenAll(fetchTasks))
                .OrderBy(x => x.Ordinal)
                .ToList();

            var zipFileName = $"{problem.Slug}.zip";

            await using var memoryStream = new MemoryStream();

            // Sequential zip writing only
            using ( var archive = new ZipArchive(memoryStream , ZipArchiveMode.Create , leaveOpen: true) )
            {
                foreach ( var item in fetchedItems )
                {
                    var ordinalFolder = $"{problem.Slug}/{item.Ordinal:D3}/";

                    var inputEntry = archive.CreateEntry(
                        $"{ordinalFolder}{problem.Slug}.inp" ,
                        CompressionLevel.Fastest);

                    await using ( var entryStream = inputEntry.Open() )
                    await using ( var writer = new StreamWriter(entryStream , new UTF8Encoding(false)) )
                    {
                        await writer.WriteAsync(item.InputText);
                    }

                    var outputEntry = archive.CreateEntry(
                        $"{ordinalFolder}{problem.Slug}.out" ,
                        CompressionLevel.Fastest);

                    await using ( var entryStream = outputEntry.Open() )
                    await using ( var writer = new StreamWriter(entryStream , new UTF8Encoding(false)) )
                    {
                        await writer.WriteAsync(item.OutputText);
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
        finally
        {
            semaphore.Dispose();
        }
    }

    private void EnsureAuthenticated()
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");
    }

    private void EnsureCanManageProblem(Problem problem)
    {
        var isAdmin = _currentUser.IsInRole("Admin") ||
              _currentUser.IsInRole("admin") ||
              _currentUser.IsInRole("teacher") ||
              _currentUser.IsInRole("manager"); ;
        if ( isAdmin ) return;

        var currentUserId = _currentUser.UserId!.Value;

        if ( problem.CreatedBy != currentUserId )
            throw new UnauthorizedAccessException("You dont have permission for this action");
    }

    private sealed class DownloadZipFetchedTestcase
    {
        public int Ordinal { get; init; }
        public string InputText { get; init; } = null!;
        public string OutputText { get; init; } = null!;
    }
}