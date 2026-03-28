using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Testsets.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System.IO.Compression;

namespace Application.UseCases.Testsets.Commands;

public sealed class UploadTestcasesZipCommandHandler
    : IRequestHandler<UploadTestcasesZipCommand , UploadTestcasesResultDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IReadRepository<Testset , Guid> _testsetReadRepository;
    private readonly IR2Service _r2Service;

    public UploadTestcasesZipCommandHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Problem , Guid> problemReadRepository ,
        IReadRepository<Testset , Guid> testsetReadRepository ,
        IR2Service r2Service)
    {
        _currentUser = currentUser;
        _problemReadRepository = problemReadRepository;
        _testsetReadRepository = testsetReadRepository;
        _r2Service = r2Service;
    }

    public async Task<UploadTestcasesResultDto> Handle(
        UploadTestcasesZipCommand request ,
        CancellationToken ct)
    {
        EnsureAuthenticated();

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if ( ext != ".zip" )
            throw new InvalidOperationException("Only .zip is supported.");

        var problem = await _problemReadRepository.GetByIdAsync(request.ProblemId , ct);
        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        EnsureCanManageProblem(problem);

        if ( !problem.IsActive || string.Equals(problem.StatusCode , "archived" , StringComparison.OrdinalIgnoreCase) )
            throw new InvalidOperationException("Problem is archived/inactive.");

        if ( string.Equals(problem.StatusCode , "published" , StringComparison.OrdinalIgnoreCase) )
            throw new InvalidOperationException("Cannot upload assets for a published problem.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            throw new InvalidOperationException("Problem.slug is required.");

        var testset = await _testsetReadRepository.GetByIdAsync(request.TestsetId , ct);
        if ( testset is null || testset.ProblemId != request.ProblemId )
            throw new KeyNotFoundException("Testset not found.");

        var tempRoot = Path.Combine(Path.GetTempPath() , "tmoj_uploads" , Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var zipPath = Path.Combine(tempRoot , "upload.zip");

            await using ( var fs = File.Create(zipPath) )
            {
                await request.FileStream.CopyToAsync(fs , ct);
            }

            var extractDir = Path.Combine(tempRoot , "extracted");
            Directory.CreateDirectory(extractDir);

            ZipFile.ExtractToDirectory(zipPath , extractDir);

            var pairs = CollectTestcasePairsByFolder(extractDir);
            if ( pairs.Count == 0 )
                throw new InvalidOperationException(
                    "No valid testcase pairs found. Expected *.inp/*.out or *-inp.txt/*-out.txt");

            var prefix = $"{request.TestsetId:D}/";

            if ( request.ReplaceExisting )
            {
                await _r2Service.DeleteByPrefixAsync("Testset" , prefix , ct);
            }

            var uploadedItems = new List<TestcaseUploadedItemDto>();

            foreach ( var kv in pairs.OrderBy(x => x.Key) )
            {
                var ordinal = kv.Key;
                var pair = kv.Value;

                var inputExt = Path.GetExtension(pair.InputPath).ToLowerInvariant();
                var outputExt = Path.GetExtension(pair.OutputPath).ToLowerInvariant();

                var inputFileName = inputExt == ".inp" ? "input.inp" : "input.txt";
                var outputFileName = outputExt == ".out" ? "output.out" : "output.txt";

                var folder = $"{request.TestsetId:D}/{ordinal:D3}/";
                var inputKey = folder + inputFileName;
                var outputKey = folder + outputFileName;

                await using ( var inputStream = File.OpenRead(pair.InputPath) )
                {
                    await _r2Service.UploadObjectAsync(
                        "Testset" ,
                        inputKey ,
                        inputStream ,
                        ResolveContentType(inputExt) ,
                        ct);
                }

                await using ( var outputStream = File.OpenRead(pair.OutputPath) )
                {
                    await _r2Service.UploadObjectAsync(
                        "Testset" ,
                        outputKey ,
                        outputStream ,
                        ResolveContentType(outputExt) ,
                        ct);
                }

                uploadedItems.Add(new TestcaseUploadedItemDto
                {
                    Ordinal = ordinal ,
                    InputObjectKey = inputKey ,
                    OutputObjectKey = outputKey
                });
            }

            return new UploadTestcasesResultDto
            {
                ProblemId = request.ProblemId ,
                Slug = problem.Slug ,
                TestsetId = request.TestsetId ,
                Total = uploadedItems.Count ,
                Items = uploadedItems
            };
        }
        finally
        {
            if ( Directory.Exists(tempRoot) )
            {
                try
                {
                    Directory.Delete(tempRoot , recursive: true);
                }
                catch
                {
                }
            }
        }
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

        // TODO: đổi CreatedBy thành field owner thực tế của entity Problem bên bạn nếu khác
        if ( problem.CreatedBy != currentUserId )
            throw new KeyNotFoundException("Problem not found or access denied.");
    }

    private static string ResolveContentType(string extension)
    {
        return extension switch
        {
            ".inp" => "text/plain",
            ".out" => "text/plain",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private static SortedDictionary<int , TestcasePair> CollectTestcasePairsByFolder(string root)
    {
        var result = new SortedDictionary<int , TestcasePair>();

        foreach ( var dir in Directory.EnumerateDirectories(root , "*" , SearchOption.AllDirectories) )
        {
            var dirName = Path.GetFileName(dir);

            if ( !int.TryParse(dirName , out var ordinal) )
                continue;

            var files = Directory.EnumerateFiles(dir).ToList();

            var inputPath =
                files.FirstOrDefault(x => string.Equals(Path.GetExtension(x) , ".inp" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => string.Equals(Path.GetFileName(x) , "input.txt" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => x.EndsWith("-inp.txt" , StringComparison.OrdinalIgnoreCase));

            var outputPath =
                files.FirstOrDefault(x => string.Equals(Path.GetExtension(x) , ".out" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => string.Equals(Path.GetFileName(x) , "output.txt" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => x.EndsWith("-out.txt" , StringComparison.OrdinalIgnoreCase));

            if ( inputPath is null || outputPath is null )
                continue;

            result[ordinal] = new TestcasePair
            {
                InputPath = inputPath ,
                OutputPath = outputPath
            };
        }

        return result;
    }

    private sealed class TestcasePair
    {
        public string InputPath { get; init; } = null!;
        public string OutputPath { get; init; } = null!;
    }
}